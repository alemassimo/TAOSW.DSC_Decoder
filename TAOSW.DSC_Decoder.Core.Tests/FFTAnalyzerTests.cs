using System.Numerics;

namespace TAOSW.DSC_Decoder.Core.Tests
{
    [TestClass]
    public class FFTAnalyzerTests
    {
        [TestMethod]
        public void ComputeFFT_WithReusableBuffer_MatchesAllocatedTransformAndClearsPadding()
        {
            var buffer = new Complex[8];
            var firstSignal = new float[] { 1, 2, 3, 4 };
            var expectedFirst = FFTAnalyzer.ComputeFFT(firstSignal, 8);
            var actual = FFTAnalyzer.ComputeFFT(firstSignal, buffer);

            Assert.AreSame(buffer, actual);
            CollectionAssert.AreEqual(expectedFirst, actual);

            var shorterSignal = new float[] { 5, 6 };
            var expectedShorter = FFTAnalyzer.ComputeFFT(shorterSignal, 8);
            actual = FFTAnalyzer.ComputeFFT(shorterSignal, buffer);

            Assert.AreSame(buffer, actual);
            CollectionAssert.AreEqual(expectedShorter, actual);
        }
    }
}
