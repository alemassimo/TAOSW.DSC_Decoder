using Avalonia;
using System;
using System.Runtime.Versioning;

namespace TAOSW.DSC_Decoder.UI
{
    internal class Program
    {
        // Initialization code. Don't use any Avalonia, third-party APIs or any
        // SynchronizationContext-reliant code before AppMain is called: things aren't initialized
        // yet and stuff might break.
        [STAThread]
        [SupportedOSPlatform("windows")]
        public static void Main(string[] args)
        {
            PlatformServices.Configure(
                sampleRate => new AudioCapture(sampleRate),
                new WindowsAudioPlaybackService());

            BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
        }

        // Avalonia configuration, don't remove; also used by visual designer.
        public static AppBuilder BuildAvaloniaApp()
            => AppBuilder.Configure<App>()
                .UsePlatformDetect()
                .WithInterFont()
                .LogToTrace();
    }
}
