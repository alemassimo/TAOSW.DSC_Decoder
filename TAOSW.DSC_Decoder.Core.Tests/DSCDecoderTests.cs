using TAOSW.DSC_Decoder.Core.TAOSW.DSC_Decoder.Core;

namespace TAOSW.DSC_Decoder.Core.Tests
{
    [TestClass]
    public class DSCDecoderTests
    {
        [TestMethod]
        public void DecodeFSK_PreservesAlignmentAcrossUnevenInputBlocks()
        {
            var decoder = new DSCDecoder(100, 44100);
            int[] expectedBitsPerBlock = [1, 1, 2, 1];
            int totalBits = 0;

            foreach (int expectedBitCount in expectedBitsPerBlock)
            {
                var bitStreams = decoder.DecodeFSK(new float[500], 300, 470);

                Assert.AreEqual(DSCDecoder.SlideWindowsNumber, bitStreams.Length);
                foreach (var bitStream in bitStreams)
                    Assert.AreEqual(expectedBitCount, bitStream.Count);

                totalBits += bitStreams[0].Count;
            }

            Assert.AreEqual(5, totalBits);
        }

        [TestMethod]
        public void DecodeFSK_ProcessesCompleteBitWindowsFromExpectedLinuxBlockSize()
        {
            var decoder = new DSCDecoder(100, 44100);

            var bitStreams = decoder.DecodeFSK(new float[882], 300, 470);

            foreach (var bitStream in bitStreams)
                Assert.AreEqual(2, bitStream.Count);
        }
    }
}
