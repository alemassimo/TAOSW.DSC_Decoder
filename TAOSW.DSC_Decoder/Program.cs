// Copyright (c) 2025 Tao Energy SRL. Licensed under the MIT License.

using TAOSW.DSC_Decoder.Core;
using TAOSW.DSC_Decoder.Core.Interfaces;

class Program
{
    private const int SampleRate = 88200;

    static void Main(string[] args)
    {
        IAudioCapture audioCapture = new AudioCapture(SampleRate);
        FskAutoTuner autoTuner = new FskAutoTuner(700, 300, SampleRate, 170);
        var squelchLevelDetector = new SquelchLevelDetector(0.0000001f, 0f);
        var devices = audioCapture.GetAudioCaptureDevices();

        Console.WriteLine("Available audio capture devices:");
        foreach (var device in devices) Console.WriteLine(device);

        Console.WriteLine("Enter the device number to start capturing or f to load a file:");
        string? s = Console.ReadLine();

        if (int.TryParse(s, out int deviceNumber))
        {
            var manager = new DscMessageManager(audioCapture, autoTuner, squelchLevelDetector, SampleRate);
            manager.OnClusteredMessageSelected += (message) =>
            {
                Console.WriteLine($"Time: {DateTime.UtcNow}");
                Console.WriteLine($"Message: {message}");
                // Qui puoi aggiungere eventuali beep o altre azioni di visualizzazione
            };
            Console.WriteLine($"Listening ...");
            manager.Start(deviceNumber);
        }
        else
        {
            Console.WriteLine("Invalid device number.");
        }
    }
}
