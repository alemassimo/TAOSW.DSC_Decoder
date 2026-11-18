using System;
using Avalonia;
using TAOSW.DSC_Decoder.Gateways.Linux;
using TAOSW.DSC_Decoder.UI;

internal static class Program
{
    [STAThread]
    public static void Main(string[] args)
    {
        PlatformServices.Configure(
            sampleRate => new LinuxAudioCapture(sampleRate),
            new LinuxAudioPlaybackService());

        BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
    }

    public static AppBuilder BuildAvaloniaApp() => AppBuilder.Configure<App>()
        .UsePlatformDetect()
        .WithInterFont()
        .LogToTrace();
}
