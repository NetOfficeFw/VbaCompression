using System.IO;

namespace Kavod.Vba.Compression.Tests
{
    public class TestMalformedCompressedData
    {
        [Test]
        public async Task EmptyCompressedContainerThrowsInvalidDataException()
        {
            await Assert.That(() => VbaCompression.Decompress([]))
                .Throws<InvalidDataException>();
        }

        [Test]
        public async Task InvalidCompressedContainerSignatureThrowsInvalidDataException()
        {
            await Assert.That(() => VbaCompression.Decompress([0x00]))
                .Throws<InvalidDataException>();
        }

        [Test]
        public async Task InvalidCompressedChunkHeaderSignatureThrowsInvalidDataException()
        {
            await Assert.That(() => VbaCompression.Decompress([0x01, 0x00, 0x00]))
                .Throws<InvalidDataException>();
        }

        [Test]
        public async Task TruncatedRawChunkThrowsInvalidDataException()
        {
            await Assert.That(() => VbaCompression.Decompress([0x01, 0xff, 0x3f, 0x42]))
                .Throws<InvalidDataException>();
        }
    }
}
