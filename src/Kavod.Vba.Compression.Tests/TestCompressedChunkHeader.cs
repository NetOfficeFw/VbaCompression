using System.Linq;

namespace Kavod.Vba.Compression.Tests
{
    public class TestCompressedChunkHeader
    {
        [Test]
        public async Task DecodeEncodeHeader()
        {
            const ushort data = 0xb250;
            byte[] expectedBytes = [0x50, 0xb2];

            var header = new CompressedChunkHeader(data);

            await Assert.That(expectedBytes.SequenceEqual(header.SerializeData())).IsTrue();
        }
    }
}
