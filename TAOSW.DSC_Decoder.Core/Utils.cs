

namespace TAOSW.DSC_Decoder.Core
{
    public static class Utils
    {
        /// <summary>
        /// 16 bit to float conversion
        /// </summary>
        /// <param name="audioData"></param>
        /// <returns></returns>
        public static float[] ConvertToFloatArray(byte[] audioData)
        {
            float[] floatData = new float[audioData.Length / 2];
            for (int i = 0; i < audioData.Length; i += 2)
            {
                short sample = (short)((audioData[i + 1] << 8) | audioData[i]);
                floatData[i / 2] = sample / 32768f;
            }
            return floatData;
        }
    }
}
