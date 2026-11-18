using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using TAOSW.DSC_Decoder.UI;

internal sealed class LinuxAudioPlaybackService : IAudioPlaybackService
{
    private static readonly string[] Players = { "paplay", "aplay" };

    public void Play(string audioFilePath)
    {
        if (!File.Exists(audioFilePath))
            throw new FileNotFoundException("Notification sound file was not found.", audioFilePath);

        string? lastFailure = null;
        foreach (var player in Players)
        {
            try
            {
                using var process = Process.Start(new ProcessStartInfo
                {
                    FileName = player,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    ArgumentList = { audioFilePath }
                }) ?? throw new InvalidOperationException($"Unable to start {player}.");

                process.WaitForExit();
                if (process.ExitCode == 0)
                    return;

                lastFailure = $"{player} exited with code {process.ExitCode}.";
            }
            catch (Win32Exception ex)
            {
                lastFailure = $"{player} is unavailable: {ex.Message}";
            }
        }

        throw new InvalidOperationException(
            $"Could not play notification sound. Install paplay (pulseaudio-utils) or aplay (alsa-utils). {lastFailure}");
    }
}
