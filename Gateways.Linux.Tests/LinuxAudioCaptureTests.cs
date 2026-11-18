using NAudio.Wave;
using TAOSW.DSC_Decoder.Gateways.Linux;

namespace Gateways.Linux.Tests;

[TestClass]
public class LinuxAudioCaptureTests
{
    [TestMethod]
    public async Task LoadAudioFileMakesPcmSamplesAvailableWithoutInitializingNativeAudio()
    {
        var filePath = Path.Combine(Path.GetTempPath(), $"dsc-audio-{Guid.NewGuid():N}.wav");
        var expectedSamples = new byte[] { 1, 2, 3, 4, 5, 6 };

        try
        {
            using (var writer = new WaveFileWriter(filePath, new WaveFormat(44100, 16, 1)))
                writer.Write(expectedSamples, 0, expectedSamples.Length);

            using var capture = new LinuxAudioCapture(44100, initializePortAudio: false);
            await capture.LoadAudioFile(filePath);

            CollectionAssert.AreEqual(expectedSamples, capture.ReadAudioData(expectedSamples.Length));
            CollectionAssert.AreEqual(Array.Empty<byte>(), capture.ReadAudioData(expectedSamples.Length));
        }
        finally
        {
            File.Delete(filePath);
        }
    }

    [TestMethod]
    public async Task LoadAudioFileDoesNotReturnAnIncompleteFinalBlock()
    {
        var filePath = Path.Combine(Path.GetTempPath(), $"dsc-audio-{Guid.NewGuid():N}.wav");
        var samples = new byte[] { 1, 2, 3, 4, 5, 6 };

        try
        {
            using (var writer = new WaveFileWriter(filePath, new WaveFormat(44100, 16, 1)))
                writer.Write(samples, 0, samples.Length);

            using var capture = new LinuxAudioCapture(44100, initializePortAudio: false);
            await capture.LoadAudioFile(filePath);

            CollectionAssert.AreEqual(Array.Empty<byte>(), capture.ReadAudioData(samples.Length + 2));
        }
        finally
        {
            File.Delete(filePath);
        }
    }

    [TestMethod]
    public void ConstructorRejectsInvalidSampleRate()
    {
        Assert.ThrowsException<ArgumentOutOfRangeException>(() => new LinuxAudioCapture(0, initializePortAudio: false));
    }

    [TestMethod]
    public void NativeDeviceAccessFailsExplicitlyWhenPortAudioWasNotInitialized()
    {
        using var capture = new LinuxAudioCapture(44100, initializePortAudio: false);

        Assert.ThrowsException<InvalidOperationException>(() => capture.GetAudioCaptureDevices());
    }
}
