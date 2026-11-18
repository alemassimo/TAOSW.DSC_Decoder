using TAOSW.DSC_Decoder.Gateways.Linux;

namespace Gateways.Linux.Tests;

[TestClass]
public class PcmRingBufferTests
{
    [TestMethod]
    public void ReadReturnsWrittenSamplesInOrderAcrossWraparound()
    {
        var buffer = new PcmRingBuffer(4);
        Assert.IsTrue(buffer.Write(new byte[] { 1, 2, 3 }));
        CollectionAssert.AreEqual(new byte[] { 1, 2 }, buffer.Read(2));
        Assert.IsTrue(buffer.Write(new byte[] { 4, 5, 6 }));

        CollectionAssert.AreEqual(new byte[] { 3, 4, 5, 6 }, buffer.Read(8));
    }

    [TestMethod]
    public void ReadExactWaitsUntilTheRequestedBlockIsAvailable()
    {
        var buffer = new PcmRingBuffer(8);
        Assert.IsTrue(buffer.Write(new byte[] { 1, 2, 3 }));

        CollectionAssert.AreEqual(Array.Empty<byte>(), buffer.ReadExact(4));

        Assert.IsTrue(buffer.Write(new byte[] { 4 }));
        CollectionAssert.AreEqual(new byte[] { 1, 2, 3, 4 }, buffer.ReadExact(4));
    }

    [TestMethod]
    public void WriteRejectsSamplesWhenBufferIsFullWithoutCorruptingExistingData()
    {
        var buffer = new PcmRingBuffer(4);
        Assert.IsTrue(buffer.Write(new byte[] { 1, 2, 3, 4 }));

        Assert.IsFalse(buffer.Write(new byte[] { 5 }));
        CollectionAssert.AreEqual(new byte[] { 1, 2, 3, 4 }, buffer.Read(8));
    }

    [TestMethod]
    public void WriteFromNativeMemoryPreservesSampleOrderAcrossWraparound()
    {
        var buffer = new PcmRingBuffer(4);
        var nativeBuffer = System.Runtime.InteropServices.Marshal.AllocHGlobal(4);

        try
        {
            System.Runtime.InteropServices.Marshal.Copy(new byte[] { 1, 2, 3 }, 0, nativeBuffer, 3);
            Assert.IsTrue(buffer.Write(nativeBuffer, 3));
            CollectionAssert.AreEqual(new byte[] { 1, 2 }, buffer.Read(2));

            System.Runtime.InteropServices.Marshal.Copy(new byte[] { 4, 5, 6 }, 0, nativeBuffer, 3);
            Assert.IsTrue(buffer.Write(nativeBuffer, 3));
            CollectionAssert.AreEqual(new byte[] { 3, 4, 5, 6 }, buffer.Read(8));
        }
        finally
        {
            System.Runtime.InteropServices.Marshal.FreeHGlobal(nativeBuffer);
        }
    }

    [TestMethod]
    public void ClearDiscardsBufferedSamples()
    {
        var buffer = new PcmRingBuffer(4);
        Assert.IsTrue(buffer.Write(new byte[] { 1, 2, 3 }));

        buffer.Clear();

        CollectionAssert.AreEqual(Array.Empty<byte>(), buffer.Read(4));
    }
}
