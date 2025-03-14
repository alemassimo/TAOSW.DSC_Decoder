using NAudio.Wave;
using TAOSW.DSC_Decoder.Core.Domain;
using TAOSW.DSC_Decoder.Core.Interfaces;

public class AudioCapture : IAudioCapture
{
    private WaveInEvent waveIn;
    private BufferedWaveProvider bufferedWaveProvider;
    private WaveFileReader waveFileReader;
    private Memory<byte> audioFileBuffer;
    private int audioFileBufferCursor = 0;
    private int _sampleRate;

    public AudioCapture(int sampleRate = 44100)
    {
        _sampleRate = sampleRate;
        audioFileBuffer = new Memory<byte>();
    }

    public List<AudioDeviceInfo> GetAudioCaptureDevices()
    {
        var devices = new List<AudioDeviceInfo>();
        for (int i = 0; i < WaveInEvent.DeviceCount; i++)
        {
            var deviceInfo = WaveInEvent.GetCapabilities(i);
            devices.Add(new AudioDeviceInfo { DeviceNumber = i, ProductName = deviceInfo.ProductName });
        }
        return devices;
    }

    public byte[] ReadAudioData(int byteCount)
    {
        if (waveFileReader != null)
        {
            //read from audioFileBuffer
            if (audioFileBufferCursor >= audioFileBuffer.Length - 1) return [];

            if (audioFileBufferCursor + byteCount > audioFileBuffer.Length - 1)
                byteCount = audioFileBuffer.Length - audioFileBufferCursor;

            var result = audioFileBuffer.Slice(audioFileBufferCursor, byteCount).ToArray();
            
            audioFileBufferCursor += byteCount;
            return result;
        }

        if (bufferedWaveProvider == null)
            throw new InvalidOperationException("Audio capture has not been started.");

        var buffer = new byte[byteCount];
        int bytesReadFromBuffer = bufferedWaveProvider.Read(buffer, 0, byteCount);

        if (bytesReadFromBuffer < byteCount)
            Array.Resize(ref buffer, bytesReadFromBuffer);

        return buffer;
    }

    public void StartAudioCapture(int deviceNumber)
    {
        waveIn = new WaveInEvent
        {
            DeviceNumber = deviceNumber,
            WaveFormat = new WaveFormat(_sampleRate, 1) // 44.1kHz mono
        };

        bufferedWaveProvider = new BufferedWaveProvider(waveIn.WaveFormat);

        waveIn.DataAvailable += (sender, e) =>
        {
            try
            {
                bufferedWaveProvider.AddSamples(e.Buffer, 0, e.BytesRecorded);
            }
            catch
            { }
        };

        waveIn.StartRecording();
    }

    public void StopAudioCapture()
    {
        if (waveIn != null)
        {
            waveIn.StopRecording();
            waveIn.Dispose();
            waveIn = null;
        }

        if (waveFileReader != null)
        {
            waveFileReader.Dispose();
            waveFileReader = null;
        }
    }

    public async Task LoadAudioFile(string filePath)
    {
        waveFileReader = new WaveFileReader(filePath);
        audioFileBuffer = new Memory<byte>(new byte[waveFileReader.Length]);
        await waveFileReader.ReadAsync(audioFileBuffer);
    }

    public BufferedWaveProvider GetBufferedWaveProvider() => bufferedWaveProvider;
}
