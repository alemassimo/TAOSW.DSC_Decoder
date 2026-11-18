using System.Runtime.InteropServices;
using System.Threading;
using NAudio.Wave;
using TAOSW.DSC_Decoder.Core.Domain;
using TAOSW.DSC_Decoder.Core.Interfaces;

namespace TAOSW.DSC_Decoder.Gateways.Linux;

public sealed class LinuxAudioCapture : IAudioCapture
{
    private const int BytesPerSample = sizeof(short);
    private readonly int _sampleRate;
    private readonly PcmRingBuffer _audioBuffer;
    private readonly object _stateLock = new();
    private readonly PortAudioNative.StreamCallback _streamCallback;
    private IntPtr _stream;
    private WaveFileReader? _waveFileReader;
    private bool _isDisposed;
    private int _isRecording;
    private int _consecutiveErrors;
    private int _bufferOverflowReported;
    private int _callbackFailureReported;
    private bool _ownsPortAudioInitialization;

    public event Action<string>? OnError;
    public event Action<string>? OnStatusChanged;

    public LinuxAudioCapture(int sampleRate = 44100) : this(sampleRate, initializePortAudio: true)
    {
    }

    internal LinuxAudioCapture(int sampleRate, bool initializePortAudio)
    {
        if (sampleRate <= 0)
            throw new ArgumentOutOfRangeException(nameof(sampleRate));

        _sampleRate = sampleRate;
        _audioBuffer = new PcmRingBuffer(checked(sampleRate * BytesPerSample * 5));
        _streamCallback = OnAudioData;

        if (initializePortAudio)
        {
            PortAudioNative.Acquire();
            _ownsPortAudioInitialization = true;
        }
    }

    public bool IsHealthy() => !_isDisposed && _consecutiveErrors < 5;

    public List<AudioDeviceInfo> GetAudioCaptureDevices()
    {
        ThrowIfDisposed();
        EnsurePortAudioInitialized();

        var devices = new List<AudioDeviceInfo>();
        var deviceCount = PortAudioNative.GetDeviceCount();
        if (deviceCount < 0)
            throw PortAudioNative.CreateException(deviceCount, "enumerate audio devices");

        for (var index = 0; index < deviceCount; index++)
        {
            var deviceInfo = PortAudioNative.GetDeviceInfo(index);
            if (deviceInfo == IntPtr.Zero)
                continue;

            var info = Marshal.PtrToStructure<PortAudioNative.DeviceInfo>(deviceInfo);
            if (info.MaxInputChannels <= 0)
                continue;

            devices.Add(new AudioDeviceInfo
            {
                DeviceNumber = index,
                ProductName = Marshal.PtrToStringUTF8(info.Name) ?? $"Audio input {index}"
            });
        }

        return devices;
    }

    public Task LoadAudioFile(string filePath)
    {
        ThrowIfDisposed();
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);

        lock (_stateLock)
        {
            if (_isRecording != 0)
                throw new InvalidOperationException("Stop live audio capture before loading an audio file.");

            var reader = new WaveFileReader(filePath);
            _waveFileReader?.Dispose();
            _waveFileReader = reader;
            _audioBuffer.Clear();
        }

        OnStatusChanged?.Invoke($"Audio file loaded: {Path.GetFileName(filePath)}");
        return Task.CompletedTask;
    }

    public byte[] ReadAudioData(int byteCount)
    {
        ThrowIfDisposed();
        if (byteCount <= 0)
            return Array.Empty<byte>();

        if (Interlocked.Exchange(ref _bufferOverflowReported, 0) != 0)
            ReportError("Audio input buffer overflowed; samples were dropped.");
        if (Interlocked.Exchange(ref _callbackFailureReported, 0) != 0)
            ReportError("The PortAudio input callback failed while processing samples.");

        byte[] audioData;
        lock (_stateLock)
        {
            if (_waveFileReader is not null)
            {
                var buffer = new byte[byteCount];
                var bytesRead = _waveFileReader.Read(buffer, 0, byteCount);
                audioData = bytesRead == byteCount ? buffer : Array.Empty<byte>();
            }
            else
            {
                audioData = _audioBuffer.ReadExact(byteCount);
            }
        }

        return audioData;
    }

    public void StartAudioCapture(int deviceNumber)
    {
        ThrowIfDisposed();
        EnsurePortAudioInitialized();

        lock (_stateLock)
        {
            if (_isRecording != 0)
                return;
            if (_waveFileReader is not null)
                throw new InvalidOperationException("Unload the audio file before starting live capture.");

            var deviceCount = PortAudioNative.GetDeviceCount();
            if (deviceNumber < 0 || deviceNumber >= deviceCount)
                throw new ArgumentOutOfRangeException(nameof(deviceNumber), $"Audio device index {deviceNumber} is unavailable.");

            var deviceInfoPointer = PortAudioNative.GetDeviceInfo(deviceNumber);
            if (deviceInfoPointer == IntPtr.Zero)
                throw new InvalidOperationException($"PortAudio returned no information for device {deviceNumber}.");

            var deviceInfo = Marshal.PtrToStructure<PortAudioNative.DeviceInfo>(deviceInfoPointer);
            if (deviceInfo.MaxInputChannels < 1)
                throw new InvalidOperationException($"Audio device {deviceNumber} does not provide an input channel.");

            var parameters = new PortAudioNative.StreamParameters
            {
                Device = deviceNumber,
                ChannelCount = 1,
                SampleFormat = PortAudioNative.SampleFormatInt16,
                SuggestedLatency = deviceInfo.DefaultHighInputLatency,
                HostApiSpecificStreamInfo = IntPtr.Zero
            };

            var error = PortAudioNative.OpenInputStream(
                out _stream,
                ref parameters,
                _sampleRate,
                _streamCallback);
            PortAudioNative.ThrowIfError(error, "open audio input");

            _audioBuffer.Clear();
            Interlocked.Exchange(ref _isRecording, 1);
            error = PortAudioNative.StartStream(_stream);
            if (error != PortAudioNative.NoError)
            {
                Interlocked.Exchange(ref _isRecording, 0);
                PortAudioNative.CloseStream(_stream);
                _stream = IntPtr.Zero;
                PortAudioNative.ThrowIfError(error, "start audio input");
            }

            Interlocked.Exchange(ref _consecutiveErrors, 0);
        }

        OnStatusChanged?.Invoke($"Audio capture started on device {deviceNumber}");
    }

    public void StopAudioCapture()
    {
        lock (_stateLock)
        {
            if (_stream != IntPtr.Zero)
            {
                Interlocked.Exchange(ref _isRecording, 0);
                var stopError = PortAudioNative.StopStream(_stream);
                var closeError = PortAudioNative.CloseStream(_stream);
                _stream = IntPtr.Zero;
                _audioBuffer.Clear();

                if (stopError != PortAudioNative.NoError && stopError != PortAudioNative.StreamIsStopped)
                    PortAudioNative.ThrowIfError(stopError, "stop audio input");
                PortAudioNative.ThrowIfError(closeError, "close audio input");
            }

            _waveFileReader?.Dispose();
            _waveFileReader = null;
        }

        OnStatusChanged?.Invoke("Audio capture stopped");
    }

    public void Dispose()
    {
        if (_isDisposed)
            return;

        try
        {
            StopAudioCapture();
        }
        finally
        {
            _isDisposed = true;
            if (_ownsPortAudioInitialization)
            {
                PortAudioNative.Release();
                _ownsPortAudioInitialization = false;
            }
        }

        GC.SuppressFinalize(this);
    }

    private int OnAudioData(IntPtr input, IntPtr output, nuint frameCount, IntPtr timeInfo, nuint statusFlags, IntPtr userData)
    {
        try
        {
            if (Volatile.Read(ref _isRecording) == 0)
                return PortAudioNative.CallbackContinue;

            if (input == IntPtr.Zero)
                return PortAudioNative.CallbackContinue;

            var byteCount = checked((int)frameCount * BytesPerSample);
            if (byteCount == 0)
                return PortAudioNative.CallbackContinue;

            if (!_audioBuffer.Write(input, byteCount))
            {
                Interlocked.Exchange(ref _bufferOverflowReported, 1);
            }

            return PortAudioNative.CallbackContinue;
        }
        catch (Exception)
        {
            Interlocked.Exchange(ref _callbackFailureReported, 1);
            return PortAudioNative.CallbackAbort;
        }
    }

    private void EnsurePortAudioInitialized()
    {
        if (!_ownsPortAudioInitialization)
            throw new InvalidOperationException("PortAudio was not initialized for this audio capture instance.");
    }

    private void ThrowIfDisposed()
    {
        ObjectDisposedException.ThrowIf(_isDisposed, this);
    }

    private void ReportError(string message)
    {
        Interlocked.Increment(ref _consecutiveErrors);
        OnError?.Invoke(message);
    }
}

internal sealed class PcmRingBuffer
{
    private readonly byte[] _buffer;
    private int _readPosition;
    private int _writePosition;

    public PcmRingBuffer(int capacity)
    {
        if (capacity < 2)
            throw new ArgumentOutOfRangeException(nameof(capacity));
        _buffer = new byte[capacity + 1];
    }

    public bool Write(byte[] data)
    {
        var readPosition = Volatile.Read(ref _readPosition);
        var writePosition = _writePosition;
        var freeBytes = (readPosition - writePosition - 1 + _buffer.Length) % _buffer.Length;
        if (data.Length > freeBytes)
            return false;

        var firstPartLength = Math.Min(data.Length, _buffer.Length - writePosition);
        Buffer.BlockCopy(data, 0, _buffer, writePosition, firstPartLength);
        var remainingLength = data.Length - firstPartLength;
        if (remainingLength > 0)
            Buffer.BlockCopy(data, firstPartLength, _buffer, 0, remainingLength);

        Volatile.Write(ref _writePosition, (writePosition + data.Length) % _buffer.Length);
        return true;
    }

    public bool Write(IntPtr source, int byteCount)
    {
        if (byteCount < 0)
            throw new ArgumentOutOfRangeException(nameof(byteCount));
        if (byteCount > 0 && source == IntPtr.Zero)
            throw new ArgumentNullException(nameof(source));
        if (byteCount == 0)
            return true;

        var readPosition = Volatile.Read(ref _readPosition);
        var writePosition = _writePosition;
        var freeBytes = (readPosition - writePosition - 1 + _buffer.Length) % _buffer.Length;
        if (byteCount > freeBytes)
            return false;

        var firstPartLength = Math.Min(byteCount, _buffer.Length - writePosition);
        Marshal.Copy(source, _buffer, writePosition, firstPartLength);
        var remainingLength = byteCount - firstPartLength;
        if (remainingLength > 0)
            Marshal.Copy(IntPtr.Add(source, firstPartLength), _buffer, 0, remainingLength);

        Volatile.Write(ref _writePosition, (writePosition + byteCount) % _buffer.Length);
        return true;
    }

    public byte[] Read(int byteCount)
    {
        return Read(byteCount, requireFullBlock: false);
    }

    public byte[] ReadExact(int byteCount)
    {
        return Read(byteCount, requireFullBlock: true);
    }

    private byte[] Read(int byteCount, bool requireFullBlock)
    {
        var writePosition = Volatile.Read(ref _writePosition);
        var readPosition = _readPosition;
        var availableBytes = (writePosition - readPosition + _buffer.Length) % _buffer.Length;
        if (requireFullBlock && availableBytes < byteCount)
            return Array.Empty<byte>();

        var bytesToRead = Math.Min(byteCount, availableBytes);
        var result = new byte[bytesToRead];

        var firstPartLength = Math.Min(bytesToRead, _buffer.Length - readPosition);
        Buffer.BlockCopy(_buffer, readPosition, result, 0, firstPartLength);
        var remainingLength = bytesToRead - firstPartLength;
        if (remainingLength > 0)
            Buffer.BlockCopy(_buffer, 0, result, firstPartLength, remainingLength);

        Volatile.Write(ref _readPosition, (readPosition + bytesToRead) % _buffer.Length);
        return result;
    }

    public void Clear()
    {
        var writePosition = Volatile.Read(ref _writePosition);
        Volatile.Write(ref _readPosition, writePosition);
    }
}

internal static class PortAudioNative
{
    private const string LibraryName = "libportaudio.so.2";
    private static readonly object InitializationLock = new();
    private static int _referenceCount;

    internal const int NoError = 0;
    internal const int StreamIsStopped = -9983;
    internal const nuint InputOverflow = 0x00000002;
    internal const nuint SampleFormatInt16 = 0x00000008;
    internal const int CallbackContinue = 0;
    internal const int CallbackAbort = 2;

    [StructLayout(LayoutKind.Sequential)]
    internal struct StreamParameters
    {
        public int Device;
        public int ChannelCount;
        public nuint SampleFormat;
        public double SuggestedLatency;
        public IntPtr HostApiSpecificStreamInfo;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct DeviceInfo
    {
        public int StructVersion;
        public IntPtr Name;
        public int HostApi;
        public int MaxInputChannels;
        public int MaxOutputChannels;
        public double DefaultLowInputLatency;
        public double DefaultLowOutputLatency;
        public double DefaultHighInputLatency;
        public double DefaultHighOutputLatency;
        public double DefaultSampleRate;
    }

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate int StreamCallback(IntPtr input, IntPtr output, nuint frameCount, IntPtr timeInfo, nuint statusFlags, IntPtr userData);

    internal static void Acquire()
    {
        lock (InitializationLock)
        {
            if (_referenceCount == 0)
            {
                int error;
                try
                {
                    error = Initialize();
                }
                catch (DllNotFoundException ex)
                {
                    throw new DllNotFoundException("PortAudio was not found. Install it with 'sudo apt install libportaudio2'.", ex);
                }

                ThrowIfError(error, "initialize PortAudio");
            }
            _referenceCount++;
        }
    }

    internal static void Release()
    {
        lock (InitializationLock)
        {
            if (_referenceCount == 0)
                return;
            _referenceCount--;
            if (_referenceCount == 0)
                ThrowIfError(Terminate(), "terminate PortAudio");
        }
    }

    internal static int GetDeviceCount() => Pa_GetDeviceCount();
    internal static IntPtr GetDeviceInfo(int device) => Pa_GetDeviceInfo(device);
    internal static int OpenInputStream(out IntPtr stream, ref StreamParameters input, double sampleRate, StreamCallback callback) =>
        Pa_OpenStream(out stream, ref input, IntPtr.Zero, sampleRate, 0, 0, callback, IntPtr.Zero);
    internal static int StartStream(IntPtr stream) => Pa_StartStream(stream);
    internal static int StopStream(IntPtr stream) => Pa_StopStream(stream);
    internal static int CloseStream(IntPtr stream) => Pa_CloseStream(stream);

    internal static void ThrowIfError(int error, string operation)
    {
        if (error != NoError)
            throw CreateException(error, operation);
    }

    internal static Exception CreateException(int error, string operation)
    {
        var message = Marshal.PtrToStringUTF8(Pa_GetErrorText(error)) ?? $"PortAudio error {error}";
        return new InvalidOperationException($"Failed to {operation}: {message}. Ensure PortAudio is installed (Debian/Ubuntu package: libportaudio2). ");
    }

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    private static extern int Pa_Initialize();
    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    private static extern int Pa_Terminate();
    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    private static extern int Pa_GetDeviceCount();
    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    private static extern IntPtr Pa_GetDeviceInfo(int device);
    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    private static extern IntPtr Pa_GetErrorText(int errorCode);
    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    private static extern int Pa_OpenStream(out IntPtr stream, ref StreamParameters inputParameters, IntPtr outputParameters, double sampleRate, nuint framesPerBuffer, nuint streamFlags, StreamCallback callback, IntPtr userData);
    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    private static extern int Pa_StartStream(IntPtr stream);
    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    private static extern int Pa_StopStream(IntPtr stream);
    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    private static extern int Pa_CloseStream(IntPtr stream);

    private static int Initialize() => Pa_Initialize();
    private static int Terminate() => Pa_Terminate();
}
