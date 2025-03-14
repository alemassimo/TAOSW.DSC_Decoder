namespace TAOSW.DSC_Decoder.Core
{
    public class DSCDecoder
    {
        private static FSKDetector FSKDetector = new FSKDetector();
        public static List<int> DecodeFSK(float[] signal, int sampleRate, float lFreq, float rFreq)
        {
            List<int> bitStream = new List<int>();
            int baudRate = 100; // DSC usa 100 Baud
            int samplesPerBit = sampleRate / baudRate;

            for (int i = 0; i < signal.Length; i += samplesPerBit)
            {
                try
                {
                    float[] chunk = new float[samplesPerBit];
                    Array.Copy(signal, i, chunk, 0, samplesPerBit);
                    int bit = FSKDetector.DetectFSK(chunk, sampleRate, lFreq, rFreq);

                    bitStream.Add(bit);
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.Message);
                }
            }

            return bitStream;
        }
    }

}
