using System;
using System.Media;
using System.Runtime.Versioning;

namespace TAOSW.DSC_Decoder.UI;

[SupportedOSPlatform("windows")]
public sealed class WindowsAudioPlaybackService : IAudioPlaybackService
{
    public void Play(string audioFilePath)
    {
        using var player = new SoundPlayer(audioFilePath)
        {
            LoadTimeout = 5000
        };
        player.Load();
        player.PlaySync();
    }
}
