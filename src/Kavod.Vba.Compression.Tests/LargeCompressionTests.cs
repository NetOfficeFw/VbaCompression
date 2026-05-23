using System.Collections.Generic;
using System.Linq;

namespace Kavod.Vba.Compression.Tests
{
    public class LargeCompressionTests
    {
        [Test]
        public async Task GivenLargeByteSequenceWithLowCompressibilityCompressionProducesContainerWithMultipleRawChunks()
        {
            var data = GetLargeByteSequenceWithLowCompressibility().ToArray();

            var container = new CompressedContainer(new DecompressedBuffer(data));

            await Assert.That(container.CompressedChunks.Count()).IsGreaterThan(1);
            await Assert.That(container.CompressedChunks.Count(c => c.Header.IsCompressed)).IsLessThanOrEqualTo(1);  // last chunk may be compressed
        }

        [Test]
        public async Task GivenLargeByteSequenceWithLowCompressibilityCompressingAndDecompressionProducesSameInput()
        {
            var data = GetLargeByteSequenceWithLowCompressibility().ToArray();

            var compressedData = VbaCompression.Compress(data);
            var convertedData = VbaCompression.Decompress(compressedData);

            await Assert.That(ReferenceEquals(data, convertedData)).IsFalse();
            await Assert.That(convertedData.LongLength).IsEqualTo(data.LongLength);
            await Assert.That(data.SequenceEqual(convertedData)).IsTrue();
        }

        private IEnumerable<byte> GetLargeByteSequenceWithLowCompressibility()
        {
            for (byte secondByte = 0; secondByte < byte.MaxValue; secondByte++)
            {
                for (byte firstByte = 0; firstByte < byte.MaxValue; firstByte++)
                {
                    if (firstByte != secondByte)
                    {
                        yield return firstByte;
                        yield return secondByte;
                    }
                }
            }
        }
    }
}
