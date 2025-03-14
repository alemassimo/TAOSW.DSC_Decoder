namespace TAOSW.DSC_Decoder.Core
{
    public class FSKDetector
    {
        public int DetectFSK(float[] signal, int sampleRate, float leftFreq, float rightFreq)
        {
            var fftResult = FFTAnalyzer.ComputeFFT(signal, 2048);
            int fftSize = fftResult.Length;

            int binLeft = (int)(leftFreq / sampleRate * fftSize);
            int binRight = (int)(rightFreq / sampleRate * fftSize);

            double powerLeft = fftResult[binLeft].Magnitude;
            double powerRight = fftResult[binRight].Magnitude;

            return powerLeft < powerRight ? 0 : 1; 
        }
    }
}
