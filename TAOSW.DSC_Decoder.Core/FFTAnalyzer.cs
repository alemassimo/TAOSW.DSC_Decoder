

using MathNet.Numerics.IntegralTransforms;
using System.Numerics;

namespace TAOSW.DSC_Decoder.Core
{
    public class FFTAnalyzer
    {
        public static Complex[] ComputeFFT(float[] signal)
        {
            int N = signal.Length;
            Complex[] fftBuffer = new Complex[N];

            for (int i = 0; i < N; i++)
                fftBuffer[i] = new Complex(signal[i], 0);

            Fourier.Forward(fftBuffer, FourierOptions.Default);
            return fftBuffer;
        }

        public static Complex[] ComputeFFT(float[] signal, int paddedLength = 0)
        {
            int N = signal.Length;
            int M = paddedLength > N ? paddedLength : N; // Usa la lunghezza maggiore tra il segnale originale e il padding

            Complex[] fftBuffer = new Complex[M];

            for (int i = 0; i < N; i++)
                fftBuffer[i] = new Complex(signal[i], 0);

            // I restanti valori sono già inizializzati a 0

            Fourier.Forward(fftBuffer, FourierOptions.Default);
            return fftBuffer;
        }
    }

}
