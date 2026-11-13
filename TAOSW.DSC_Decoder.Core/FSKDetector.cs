namespace TAOSW.DSC_Decoder.Core
{
    using System.Numerics;

    public class FSKDetector
    {
        private readonly int _sampleRate;
        private readonly object _bufferLock = new();
        private float[] _windowedSignal = Array.Empty<float>();
        private Complex[] _fftBuffer = new Complex[2048];

        public FSKDetector(int sampleRate)
        {
            _sampleRate = sampleRate;
        }

        public int DetectFSK(float[] inSignal, float leftFreq, float rightFreq, out double degreeOfCertainty)
        {
            lock (_bufferLock)
            {
                if (_windowedSignal.Length != inSignal.Length)
                    _windowedSignal = new float[inSignal.Length];

                int fftSize = Math.Max(2048, inSignal.Length);
                if (_fftBuffer.Length != fftSize)
                    _fftBuffer = new Complex[fftSize];

                ApplyHannWindow(inSignal, _windowedSignal);
                var fftResult = FFTAnalyzer.ComputeFFT(_windowedSignal, _fftBuffer);

                int binLeft = (int)(leftFreq / _sampleRate * fftSize);
                int binRight = (int)(rightFreq / _sampleRate * fftSize);

                double powerLeft = fftResult[binLeft].Magnitude;
                double powerRight = fftResult[binRight].Magnitude;
                degreeOfCertainty = Math.Abs(powerLeft - powerRight) / Math.Max(powerLeft, powerRight);

                return powerLeft < powerRight ? 0 : 1;
            }
        }

        private static void ApplyHannWindow(float[] data, float[] output)
        {
            for (int i = 0; i < data.Length; i++)
                output[i] = data[i] * 0.5f * (1 - MathF.Cos(2 * MathF.PI * i / (data.Length - 1)));
        }
    }
}
