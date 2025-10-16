// Copyright (c) 2025 Tao Energy SRL. Licensed under the MIT License.

using NAudio.Wave;
using TAOSW.DSC_Decoder.Core.Domain;
using TAOSW.DSC_Decoder.Core.Interfaces;
using System.Diagnostics;

public class AudioCapture : IAudioCapture, IDisposable
{
    private WaveInEvent? waveIn;
    private BufferedWaveProvider? bufferedWaveProvider;
    private WaveFileReader? waveFileReader;
    private Memory<byte> audioFileBuffer;
    private int audioFileBufferCursor = 0;
    private int _sampleRate;
    private volatile bool _isDisposed = false;
    private volatile bool _isRecording = false;
    private readonly object _lockObject = new object();
    
    // Error tracking and recovery
    private int _consecutiveErrors = 0;
    private const int MaxConsecutiveErrors = 5;
    private DateTime _lastErrorTime = DateTime.MinValue;
    private readonly TimeSpan _errorResetInterval = TimeSpan.FromMinutes(1);

    public event Action<string>? OnError;
    public event Action<string>? OnStatusChanged;

    public AudioCapture(int sampleRate = 44100)
    {
        _sampleRate = sampleRate;
        audioFileBuffer = new Memory<byte>();
    }

    public List<AudioDeviceInfo> GetAudioCaptureDevices()
    {
        try
        {
            var devices = new List<AudioDeviceInfo>();
            for (int i = 0; i < WaveInEvent.DeviceCount; i++)
            {
                try
                {
                    var deviceInfo = WaveInEvent.GetCapabilities(i);
                    devices.Add(new AudioDeviceInfo { DeviceNumber = i, ProductName = deviceInfo.ProductName });
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error getting device {i} capabilities: {ex.Message}");
                    // Continue with other devices
                }
            }
            return devices;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error enumerating audio devices: {ex.Message}");
            OnError?.Invoke($"Failed to enumerate audio devices: {ex.Message}");
            return new List<AudioDeviceInfo>();
        }
    }

    public byte[] ReadAudioData(int byteCount)
    {
        if (_isDisposed)
            throw new ObjectDisposedException(nameof(AudioCapture));

        try
        {
            if (waveFileReader != null)
            {
                return ReadFromFile(byteCount);
            }

            return ReadFromBuffer(byteCount);
        }
        catch (Exception ex)
        {
            HandleError($"Error reading audio data: {ex.Message}");
            return Array.Empty<byte>();
        }
    }

    private byte[] ReadFromFile(int byteCount)
    {
        if (audioFileBufferCursor >= audioFileBuffer.Length - 1) 
            return Array.Empty<byte>();

        if (audioFileBufferCursor + byteCount > audioFileBuffer.Length - 1)
            byteCount = audioFileBuffer.Length - audioFileBufferCursor;

        var result = audioFileBuffer.Slice(audioFileBufferCursor, byteCount).ToArray();
        audioFileBufferCursor += byteCount;
        return result;
    }

    private byte[] ReadFromBuffer(int byteCount)
    {
        lock (_lockObject)
        {
            if (bufferedWaveProvider == null)
            {
                HandleError("Audio capture buffer is not available");
                return Array.Empty<byte>();
            }

            var buffer = new byte[byteCount];
            int bytesReadFromBuffer = bufferedWaveProvider.Read(buffer, 0, byteCount);

            if (bytesReadFromBuffer < byteCount)
                Array.Resize(ref buffer, bytesReadFromBuffer);

            return buffer;
        }
    }

    public void StartAudioCapture(int deviceNumber)
    {
        if (_isDisposed)
            throw new ObjectDisposedException(nameof(AudioCapture));

        try
        {
            lock (_lockObject)
            {
                if (_isRecording)
                {
                    Console.WriteLine("Audio capture is already running");
                    return;
                }

                // Validate device number
                if (deviceNumber < 0 || deviceNumber >= WaveInEvent.DeviceCount)
                {
                    throw new ArgumentOutOfRangeException(nameof(deviceNumber), 
                        $"Device number {deviceNumber} is out of range. Available devices: 0-{WaveInEvent.DeviceCount - 1}");
                }

                // Create new WaveInEvent with error handling
                waveIn = new WaveInEvent
                {
                    DeviceNumber = deviceNumber,
                    WaveFormat = new WaveFormat(_sampleRate, 1), // mono
                    BufferMilliseconds = 100 // Reduce buffer size for lower latency
                };

                // Configure buffered wave provider with appropriate buffer size
                bufferedWaveProvider = new BufferedWaveProvider(waveIn.WaveFormat)
                {
                    BufferLength = _sampleRate * 2 * 5, // 5 seconds buffer
                    DiscardOnBufferOverflow = true // Prevent memory buildup
                };

                // Add comprehensive event handlers
                waveIn.DataAvailable += OnDataAvailable;
                waveIn.RecordingStopped += OnRecordingStopped;

                // Start recording
                waveIn.StartRecording();
                _isRecording = true;
                
                OnStatusChanged?.Invoke($"Audio capture started on device {deviceNumber}");
                Console.WriteLine($"Successfully started audio capture on device {deviceNumber}");
                
                // Reset error counter on successful start
                _consecutiveErrors = 0;
            }
        }
        catch (Exception ex)
        {
            HandleError($"Failed to start audio capture on device {deviceNumber}: {ex.Message}");
            CleanupWaveIn();
            throw;
        }
    }

    private void OnDataAvailable(object? sender, WaveInEventArgs e)
    {
        try
        {
            lock (_lockObject)
            {
                if (bufferedWaveProvider != null && !_isDisposed && e.BytesRecorded > 0)
                {
                    bufferedWaveProvider.AddSamples(e.Buffer, 0, e.BytesRecorded);
                }
            }
        }
        catch (Exception ex)
        {
            HandleError($"Error processing audio data: {ex.Message}");
        }
    }

    private void OnRecordingStopped(object? sender, StoppedEventArgs e)
    {
        _isRecording = false;
        
        if (e.Exception != null)
        {
            HandleError($"Recording stopped due to error: {e.Exception.Message}");
        }
        else
        {
            OnStatusChanged?.Invoke("Recording stopped normally");
            Console.WriteLine("Audio recording stopped");
        }
    }

    private void HandleError(string errorMessage)
    {
        Console.WriteLine($"Audio Capture Error: {errorMessage}");
        OnError?.Invoke(errorMessage);
        
        _consecutiveErrors++;
        _lastErrorTime = DateTime.Now;
        
        // If too many consecutive errors, stop recording to prevent resource leaks
        if (_consecutiveErrors >= MaxConsecutiveErrors)
        {
            Console.WriteLine($"Too many consecutive errors ({_consecutiveErrors}). Stopping audio capture.");
            StopAudioCapture();
            OnError?.Invoke($"Audio capture stopped due to {_consecutiveErrors} consecutive errors");
        }
    }

    public void StopAudioCapture()
    {
        try
        {
            lock (_lockObject)
            {
                _isRecording = false;
                CleanupWaveIn();
                
                if (waveFileReader != null)
                {
                    waveFileReader.Dispose();
                    waveFileReader = null;
                }
                
                OnStatusChanged?.Invoke("Audio capture stopped");
                Console.WriteLine("Audio capture stopped successfully");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error stopping audio capture: {ex.Message}");
        }
    }

    private void CleanupWaveIn()
    {
        if (waveIn != null)
        {
            try
            {
                waveIn.DataAvailable -= OnDataAvailable;
                waveIn.RecordingStopped -= OnRecordingStopped;
                
                if (_isRecording)
                {
                    waveIn.StopRecording();
                }
                
                waveIn.Dispose();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error disposing WaveIn: {ex.Message}");
            }
            finally
            {
                waveIn = null;
            }
        }
        
        bufferedWaveProvider = null;
    }

    public async Task LoadAudioFile(string filePath)
    {
        if (_isDisposed)
            throw new ObjectDisposedException(nameof(AudioCapture));

        try
        {
            if (string.IsNullOrEmpty(filePath) || !File.Exists(filePath))
            {
                throw new FileNotFoundException($"Audio file not found: {filePath}");
            }

            waveFileReader = new WaveFileReader(filePath);
            
            // Check file format compatibility
            if (waveFileReader.WaveFormat.SampleRate != _sampleRate)
            {
                Console.WriteLine($"Warning: File sample rate ({waveFileReader.WaveFormat.SampleRate}) differs from expected ({_sampleRate})");
            }

            audioFileBuffer = new Memory<byte>(new byte[waveFileReader.Length]);
            await waveFileReader.ReadAsync(audioFileBuffer);
            audioFileBufferCursor = 0;
            
            Console.WriteLine($"Successfully loaded audio file: {filePath}");
            OnStatusChanged?.Invoke($"Audio file loaded: {Path.GetFileName(filePath)}");
        }
        catch (Exception ex)
        {
            HandleError($"Failed to load audio file {filePath}: {ex.Message}");
            throw;
        }
    }

    public BufferedWaveProvider? GetBufferedWaveProvider() => bufferedWaveProvider;

    public bool IsRecording => _isRecording;
    
    public bool IsHealthy()
    {
        // Reset error count if enough time has passed
        if (DateTime.Now - _lastErrorTime > _errorResetInterval)
        {
            _consecutiveErrors = 0;
        }
        
        return _consecutiveErrors < MaxConsecutiveErrors && !_isDisposed;
    }

    public void Dispose()
    {
        if (!_isDisposed)
        {
            _isDisposed = true;
            StopAudioCapture();
            GC.SuppressFinalize(this);
        }
    }

    ~AudioCapture()
    {
        Dispose();
    }
}
