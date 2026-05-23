using System;
using System.Linq;

namespace Kavod.Vba.Compression.Tests
{
    public class TestCompressedChunkHeader
    {
        [Test]
        public async Task DecodeEncodeHeader()
        {
            var data = BitConverter.ToUInt16(new byte[] {
            0x50,
            0xb2
            }, 0);
            var header = new CompressedChunkHeader(data);

            await Assert.That(BitConverter.GetBytes(data).SequenceEqual(header.SerializeData())).IsTrue();
        }
    }
}
