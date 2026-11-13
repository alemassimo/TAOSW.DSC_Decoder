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
            private readonly float[] _signalQueue;
            private readonly float[] _chunk;
            private float[] _processedSignal = Array.Empty<float>();
            private readonly FSKDetector FskDetector;

            public DSCDecoder(int baudRate, int sampleRate)
            {
                _sampleRate = sampleRate;
                _samplesPerBit = sampleRate / baudRate;
                _windowSlip =(int)(_samplesPerBit* MaxSlideWindowOffset) / (SlideWindowsNumber);
                _signalQueue = new float[_samplesPerBit];
                _chunk = new float[_samplesPerBit];
                FskDetector = new FSKDetector(_sampleRate);

                InitSignalQueue();
            }
            public List<int>[] DecodeFSK(float[] inSignal, float lFreq, float rFreq)
            {
                List<int>[] bitStreams = InitBitStreams();

                int processedSignalLength = inSignal.Length + _samplesPerBit;
                if (_processedSignal.Length != processedSignalLength)
                    _processedSignal = new float[processedSignalLength];

                Array.Copy(inSignal, 0, _processedSignal, _samplesPerBit, inSignal.Length);
                Array.Copy(_signalQueue, 0, _processedSignal, 0, _samplesPerBit);

                for (int i = 0; i < inSignal.Length; i += _samplesPerBit)
                    for (int j = 0; j < SlideWindowsNumber; j++)
                        bitStreams[j].Add(RetriveBitByWindowNumber(lFreq, rFreq, _processedSignal, i, j, out _));

                Array.Copy(_processedSignal, inSignal.Length, _signalQueue, 0, _samplesPerBit);

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

            private void InitSignalQueue()
            {
                for (int i = 0; i < _samplesPerBit; i++) _signalQueue[i] = 0;
            }
        }

    }
}
