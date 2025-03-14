using MathNet.Numerics;
using System;
using TAOSW.DSC_Decoder.Core;
using TAOSW.DSC_Decoder.Core.Interfaces;

class Program
{
    private const int SampleRate = 88200;

    static void Main(string[] args)
    {
        IAudioCapture audioCapture = new AudioCapture(SampleRate);
        AutoTuner autoTuner = new AutoTuner(700, 300, SampleRate,170);
        var squelchLevelDetector = new SquelchLevelDetector(0.0000001f, 0f);
        var devices = audioCapture.GetAudioCaptureDevices();
        var decoder = new GMDSSDecoder();

        Console.WriteLine("Available audio capture devices:");
        foreach (var device in devices)Console.WriteLine(device);
        
        Console.WriteLine("Enter the device number to start capturing or f to load a file:");
        string? s = Console.ReadLine();

        if (int.TryParse(s, out int deviceNumber))
        {
            audioCapture.StartAudioCapture(deviceNumber);
            Console.WriteLine($"Listening ...");
            decoder.OnMessageDecoded += (message) => Console.WriteLine(message.ToString());
            while (true)
            {
                if (Console.KeyAvailable)
                {
                    var keyInfo = Console.ReadKey(true);
                    // se ha premuto la lettera 'e' allora va in break 
                    if (keyInfo.Key == ConsoleKey.E) break;
                    // clear screen 
                    if (keyInfo.Key == ConsoleKey.C) Console.Clear();
                }
                var result = audioCapture.ReadAudioData(3528); //1764 multiple
                float[] signal = Utils.ConvertToFloatArray(result);
               
                if (!squelchLevelDetector.Detect(signal))continue;

                float[] processedSignal = autoTuner.ProcessSignal(signal);

                //var bits = DSCDecoder.DecodeFSK(signal, SampleRate, 409.0f, 590.0f);
                //var bits = DSCDecoder.DecodeFSK(processedSignal, SampleRate,409  ,579);
                var bits = DSCDecoder.DecodeFSK(processedSignal, SampleRate, autoTuner.LeftFreq, autoTuner.RightFreq);
                decoder.AddBits(bits);
            }

            audioCapture.StopAudioCapture();

        }
        else
        {
            Console.WriteLine("Invalid device number.");
        }
    }

    
}
