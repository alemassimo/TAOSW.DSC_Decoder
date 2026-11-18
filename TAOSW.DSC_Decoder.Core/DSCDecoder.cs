using MathNet.Numerics;

namespace TAOSW.DSC_Decoder.Core
{
    using System;
    using System.Collections.Generic;

    namespace TAOSW.DSC_Decoder.Core
    {
        public class DSCDecoder
        {
            public const int SlideWindowsNumber = 3;
            private const float MaxSlideWindowOffset = 0.5f;
            private readonly int _samplesPerBit;
            private readonly int _sampleRate;
            private readonly int _windowSlip;
            private readonly float[] _chunk;
            private float[] _pendingSignal;
            private readonly FSKDetector FskDetector;

            public DSCDecoder(int baudRate, int sampleRate)
            {
                _sampleRate = sampleRate;
                _samplesPerBit = sampleRate / baudRate;
                _windowSlip = (int)(_samplesPerBit * MaxSlideWindowOffset) / SlideWindowsNumber;
                _pendingSignal = new float[_samplesPerBit];
                _chunk = new float[_samplesPerBit];
                FskDetector = new FSKDetector(_sampleRate);
            }

            public List<int>[] DecodeFSK(float[] inSignal, float lFreq, float rFreq)
            {
                ArgumentNullException.ThrowIfNull(inSignal);

                List<int>[] bitStreams = InitBitStreams();
                var signal = new float[_pendingSignal.Length + inSignal.Length];
                Array.Copy(_pendingSignal, signal, _pendingSignal.Length);
                Array.Copy(inSignal, 0, signal, _pendingSignal.Length, inSignal.Length);

                int requiredWindowLength = _samplesPerBit + (SlideWindowsNumber - 1) * _windowSlip;
                int processedSamples = 0;
                for (; processedSamples + requiredWindowLength <= signal.Length; processedSamples += _samplesPerBit)
                {
                    for (int window = 0; window < SlideWindowsNumber; window++)
                    {
                        bitStreams[window].Add(RetriveBitByWindowNumber(lFreq, rFreq, signal, processedSamples, window, out _));
                    }
                }

                _pendingSignal = new float[signal.Length - processedSamples];
                Array.Copy(signal, processedSamples, _pendingSignal, 0, _pendingSignal.Length);

                return bitStreams;
            }

            private static List<int>[] InitBitStreams()
            {
                var bitStreams = new List<int>[SlideWindowsNumber];
                for (int n = 0; n < SlideWindowsNumber; n++) bitStreams[n] = new List<int>();
                return bitStreams;
            }

            private int RetriveBitByWindowNumber(float lFreq, float rFreq, float[] signal, int i, int windowNumber, out double certainty)
            {
                Array.Copy(signal, i + windowNumber * _windowSlip, _chunk, 0, _samplesPerBit);
                return FskDetector.DetectFSK(_chunk, lFreq, rFreq, out certainty);
            }
        }

    }
}
