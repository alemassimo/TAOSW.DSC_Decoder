using System;
using TAOSW.DSC_Decoder.Core.Interfaces;

namespace TAOSW.DSC_Decoder.UI;

public interface IAudioPlaybackService
{
    void Play(string audioFilePath);
}

public static class PlatformServices
{
    private static Func<int, IAudioCapture>? _audioCaptureFactory;
    private static IAudioPlaybackService? _audioPlaybackService;

    public static void Configure(Func<int, IAudioCapture> audioCaptureFactory, IAudioPlaybackService audioPlaybackService)
    {
        _audioCaptureFactory = audioCaptureFactory ?? throw new ArgumentNullException(nameof(audioCaptureFactory));
        _audioPlaybackService = audioPlaybackService ?? throw new ArgumentNullException(nameof(audioPlaybackService));
    }

    public static IAudioCapture CreateAudioCapture(int sampleRate) =>
        (_audioCaptureFactory ?? throw new InvalidOperationException("The platform audio capture service has not been configured."))(sampleRate);

    public static IAudioPlaybackService AudioPlayback =>
        _audioPlaybackService ?? throw new InvalidOperationException("The platform audio playback service has not been configured.");
}
