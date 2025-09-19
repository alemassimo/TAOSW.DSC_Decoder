

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
            int M = paddedLength > N ? paddedLength : N; 

            Complex[] fftBuffer = new Complex[M];

            for (int i = 0; i < N; i++)
                fftBuffer[i] = new Complex(signal[i], 0);

            Fourier.Forward(fftBuffer, FourierOptions.Default);
            return fftBuffer;
        }

        /// <summary>
        /// Calcola spettro in dB (ampiezza) e asse delle frequenze.
        /// </summary>
        /// <param name="signal">Campioni temporali (float)</param>
        /// <param name="sampleRate">Sample rate (Hz)</param>
        /// <param name="singleSided">True = usa solo metà spettro (segnali reali)</param>
        /// <param name="windowType">Tipo finestra (None/Hann)</param>
        /// <param name="referenceAmplitude">
        /// Se null => usa 1.0 come riferimento. 
        /// Se &lt;=0 viene forzato a 1.0
        /// </param>
        /// <returns>(frequenze[], ampiezzaDb[])</returns>
        public static (double[] freq, double[] ampDb) ComputeSpectrumDb(
            float[] signal,
            int sampleRate,
            bool singleSided = true,
            WindowType windowType = WindowType.Hann,
            double? referenceAmplitude = 1.0)
        {
            int N = signal.Length;
            if (N == 0) return (Array.Empty<double>(), Array.Empty<double>());

            // Applica finestra (se richiesta)
            double[] window = windowType switch
            {
                WindowType.Hann => BuildHannWindow(N),
                _ => Enumerable.Repeat(1.0, N).ToArray()
            };

            // Coherent gain (media della finestra) per correggere l’attenuazione
            double coherentGain = window.Average();

            float[] windowed = new float[N];
            for (int i = 0; i < N; i++)
                windowed[i] = (float)(signal[i] * window[i]);

            var fft = ComputeFFT(windowed);

            int usefulLength = singleSided ? (N / 2) + 1 : N;
            double[] freqs = new double[usefulLength];
            double[] ampDb = new double[usefulLength];

            double refAmp = referenceAmplitude.HasValue && referenceAmplitude.Value > 0
                ? referenceAmplitude.Value
                : 1.0;

            const double floor = 1e-12;

            for (int k = 0; k < usefulLength; k++)
            {
                // Ampiezza lineare normalizzata: |X[k]| / (N * coherentGain)
                double mag = fft[k].Magnitude / (N * coherentGain);

                // Correzione single-sided (tranne DC e Nyquist se N pari)
                if (singleSided && k > 0 && !(N % 2 == 0 && k == N / 2))
                    mag *= 2.0;

                // Evita log(0)
                mag = Math.Max(mag, floor);

                ampDb[k] = 20.0 * Math.Log10(mag / refAmp);
                freqs[k] = k * (double)sampleRate / N;
            }

            return (freqs, ampDb);
        }

        /// <summary>
        /// Variante per potenza in dB (10*log10(P/Pref))
        /// </summary>
        public static (double[] freq, double[] powerDb) ComputePowerSpectrumDb(
            float[] signal,
            int sampleRate,
            bool singleSided = true,
            WindowType windowType = WindowType.Hann,
            double? referencePower = 1.0)
        {
            var (freqs, ampDb) = ComputeSpectrumDb(signal, sampleRate, singleSided, windowType, 1.0);
            // ampDb = 20 log10(A). Per passare alla potenza: P ∝ A^2.
            // dB_P = 10 log10(P/Pref) = 20 log10(A) - 10 log10(Pref).
            double refP = referencePower.HasValue && referencePower.Value > 0 ? referencePower.Value : 1.0;
            double refCorr = 10 * Math.Log10(refP);
            double[] powerDb = new double[ampDb.Length];
            for (int i = 0; i < ampDb.Length; i++)
                powerDb[i] = ampDb[i] - refCorr;
            return (freqs, powerDb);
        }

        private static double[] BuildHannWindow(int N)
        {
            double[] w = new double[N];
            if (N == 1)
            {
                w[0] = 1;
                return w;
            }
            for (int n = 0; n < N; n++)
                w[n] = 0.5 * (1 - Math.Cos(2 * Math.PI * n / (N - 1)));
            return w;
        }

        public enum WindowType
        {
            None,
            Hann
        }

    }

}
