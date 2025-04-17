using MathNet.Numerics;
using System;
using System.Text;
using TAOSW.DSC_Decoder.Core;
using TAOSW.DSC_Decoder.Core.Interfaces;
using TAOSW.DSC_Decoder.Core.TAOSW.DSC_Decoder.Core;

class Program
{
    private const int SampleRate = 88200;

    static void Main(string[] args)
    {
        IAudioCapture audioCapture = new AudioCapture(SampleRate);
        FskAutoTuner autoTuner = new FskAutoTuner(700, 300, SampleRate,170);
        var squelchLevelDetector = new SquelchLevelDetector(0.0000001f, 0f);
        var devices = audioCapture.GetAudioCaptureDevices();
        var decoders = new GMDSSDecoder[DSCDecoder.SlideWindowsNumber];
        DSCDecoder dSCDecoder = new DSCDecoder(100, SampleRate);

        Console.WriteLine("Available audio capture devices:");
        foreach (var device in devices)Console.WriteLine(device);
        
        Console.WriteLine("Enter the device number to start capturing or f to load a file:");
        string? s = Console.ReadLine();

        if (int.TryParse(s, out int deviceNumber))
        {
            audioCapture.StartAudioCapture(deviceNumber);
            Console.WriteLine($"Listening ...");
            for(int i = 0; i < DSCDecoder.SlideWindowsNumber; i++)
            {
                decoders[i] = new GMDSSDecoder();
                decoders[i].OnMessageDecoded += (message) =>
                {
                    Console.WriteLine(DateTime.Now);
                    Console.WriteLine(message.ToString());
                };
            }
            
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
                var result = audioCapture.ReadAudioData(1764); //1764 multiple
                float[] signal = Utils.ConvertToFloatArray(result);
               
                if (!squelchLevelDetector.Detect(signal))continue;

                float[] processedSignal = autoTuner.ProcessSignal(signal);

                var bits = dSCDecoder.DecodeFSK(processedSignal, autoTuner.LeftFreq, autoTuner.RightFreq);
                for(int i = 0; i < DSCDecoder.SlideWindowsNumber; i++) decoders[i].AddBits(bits[i]);
                
            }
            audioCapture.StopAudioCapture();
        }
        else
        {
            Console.WriteLine("Invalid device number.");
        }
    }

    
}
