using System.Numerics;

namespace TAOSW.DSC_Decoder.Core
{
    public class FskAutoTuner
    {
        private const double MagnitudeThreshold = 0.1;
        private Dictionary<float, int> freqPowerDictionary = [];
        private readonly Queue<float[]> signalBuffer = new();
        private float autoLeftFreq;
        private float autoRightFreq;
        private readonly float maxFreq;
        private readonly float minFreq;
        private readonly int sampleRate;
        private readonly float shift;

        public event Action<IEnumerable<FrequencyClusterPower>> OnFrequenciesDetected;

        public FskAutoTuner(float maxFreq, float minFreq, int sampleRate, float shift)
        {
            this.maxFreq = maxFreq;
            this.minFreq = minFreq;
            this.sampleRate = sampleRate;
            this.shift = shift;
        }

        public float LeftFreq => autoLeftFreq;
        public float RightFreq => autoRightFreq;

        public float[] ProcessSignal(float[] signal)
        {
            signalBuffer.Enqueue(signal);
            
            ProcessSignalInt();
            if (signalBuffer.Count >= 2) return signalBuffer.Dequeue();
            
            return [];
        }

        private void ProcessSignalInt()
        {
            float[] signal = [];

            foreach (var s in signalBuffer) signal = signal.Concat(s).ToArray();

            var fftResult = FFTAnalyzer.ComputeFFT(signal, 2048);
            int fftSize = fftResult.Length;

            var freqs = ExtractsFrequencies(fftResult, sampleRate);

            OnFrequenciesDetected?.Invoke(freqs);

            var peaks = freqs.OrderByDescending(f => f.Power).Take(2).ToList();

            if (peaks.Count < 2) return; 

            double f1 = peaks[0].Frequency;
            double f2 = peaks[1].Frequency;

            if (Math.Abs(f2 - f1) < shift - 10 || Math.Abs(f2 - f1) > shift + 10) return; 

            float lF = (float)(f1 < f2 ? f1 : f2);
            float rF = (float)(f1 < f2 ? f2 : f1);

            if (Math.Abs(peaks[0].Power - peaks[1].Power) < MagnitudeThreshold * Math.Max(peaks[0].Power, peaks[1].Power)) return;
            //if (Math.Abs(autoLeftFreq - lF) <= 25) return; 
            
            autoLeftFreq = lF;
            autoRightFreq = rF;
        }

        private List<FrequencyClusterPower> ExtractsFrequencies(Complex[] fftResult, int sampleRate)
        {
            var freqs = new List<FrequencyClusterPower>();
            int fftSize = fftResult.Length;

            for (int i = 0; i < fftSize / 2; i++)
            {
                float _frequency = (float)i / fftSize * sampleRate;
                if (_frequency < minFreq || _frequency > maxFreq) continue;
                double power = fftResult[i].Magnitude;
                freqs.Add(new FrequencyClusterPower() { Frequency = _frequency, Power = power });
            }
            return freqs;
        }

        public class FrequencyClusterPower
        {
            public float Frequency { get; set; }
            public double Power { get; set; }
        }
    }

    
}
