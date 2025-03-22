namespace TAOSW.DSC_Decoder.Core.Tests
{
    [TestClass]
    public class GMDSSDecoderTests
    {
        [TestMethod]
        public void RetriveDataByteTest1()
        {
            var bits = new List<int> { 0, 1, 0, 0, 0, 0, 0, 1, 1, 0 };
            var i = 0;
            var expected = 2;
            var actual = GMDSSDecoder.RetriveDataByte(bits, i);
            Assert.AreEqual(expected, actual);
        }

        [TestMethod]
        public void RetriveDataByteTest2()
        {
            var bits = new List<int> { 0, 1, 0, 1, 1, 1, 1, 0, 1, 0 };
            var i = 0;
            var expected = 122;
            var actual = GMDSSDecoder.RetriveDataByte(bits, i);
            Assert.AreEqual(expected, actual);
        }

        [TestMethod]
        public void RetriveDataByteTest3()
        {
            var bits = new List<int> { 1, 1, 1, 1, 1, 1, 1, 0, 0, 0 };
            var i = 0;
            var expected = 127;
            var actual = GMDSSDecoder.RetriveDataByte(bits, i);
            Assert.AreEqual(expected, actual);
        }

        [TestMethod]
        public void RetriveDataByteTest4()
        {
            //YYBYBYBBYY
            var bits = new List<int> { 1, 1, 0, 1, 0, 1, 0, 0, 1, 1 };
            var i = 0;
            var expected = 43;
            var actual = GMDSSDecoder.RetriveDataByte(bits, i);
            Assert.AreEqual(expected, actual);
        }

        [TestMethod]
        public void IsPreambleGroupTest()
        {
            var bits = new List<int> { 1, 0, 1, 0, 1, 0, 1, 0, 1, 0 };
            var expected = true;
            var actual = GMDSSDecoder.IsPreambleGroup(bits.ToArray(),0);
            Assert.AreEqual(expected, actual);
        }

        [TestMethod]
        public void IsPreambleGroupTest2()
        {
            var bits = new List<int> { 0, 1, 0, 1, 0, 1, 0, 1, 0, 1 };
            var expected = true;
            var actual = GMDSSDecoder.IsPreambleGroup(bits.ToArray(), 0);
            Assert.AreEqual(expected, actual);
        }

        [TestMethod]
        public void IsPreambleGroupTest3()
        {
            var bits = new List<int> { 0, 1, 1, 1, 0, 1, 0, 1, 0, 1 };
            var expected = false;
            var actual = GMDSSDecoder.IsPreambleGroup(bits.ToArray(), 0);
            Assert.AreEqual(expected, actual);
        }
    }

    //0101111010 122
}