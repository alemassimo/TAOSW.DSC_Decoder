namespace TAOSW.DSC_Decoder.Core
{
    public class FSKDetector
    {
        private readonly int _sampleRate;

        public FSKDetector(int sampleRate)
        {
            _sampleRate = sampleRate;
        }

        public int DetectFSK(float[] inSignal, float leftFreq, float rightFreq, out double degreeOfCertainty)
        {
            var signal = ApplyHannWindow(inSignal);
            var fftResult = FFTAnalyzer.ComputeFFT(signal, 2048);
            int fftSize = fftResult.Length;

            int binLeft = (int)(leftFreq / _sampleRate * fftSize);
            int binRight = (int)(rightFreq / _sampleRate * fftSize);

            double powerLeft = fftResult[binLeft].Magnitude;
            double powerRight = fftResult[binRight].Magnitude;
            degreeOfCertainty = Math.Abs(powerLeft - powerRight) / Math.Max(powerLeft, powerRight);
            
            return powerLeft < powerRight ? 0 : 1; 
        }

        private static float[] ApplyHannWindow(float[] data)
        {
            float[] outSignal = new float[data.Length];
            for (int i = 0; i < data.Length; i++)
                outSignal[i] = data[i] * 0.5f * (1 - MathF.Cos(2 * MathF.PI * i / (data.Length - 1)));
            return outSignal;
        }
    }
}
