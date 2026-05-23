using System.Collections.Generic;
using System.Linq;

namespace Kavod.Vba.Compression.Tests
{
    public static class CompressionTestHelper
    {
        public static async Task LowLevelCompressionComparison(byte[] decompressedBytes, byte[] expectedCompressedBytes)
        {
            var refCompressed = new CompressedContainer(expectedCompressedBytes);
            var decompressed = new DecompressedBuffer(decompressedBytes);
            var sutCompressed = new CompressedContainer(decompressed);

            var refTokens = GetTokensFromCompressedContainer(refCompressed).OfType<CopyToken>().ToList();
            var sutTokens = GetTokensFromCompressedContainer(sutCompressed).OfType<CopyToken>().ToList();

            for (var i = 0; i < refTokens.Count; i++)
            {
                var expected = refTokens[i];
                var actual = sutTokens[i];
                await Assert.That(actual).IsEqualTo(expected);
            }
        }

        private static IEnumerable<IToken> GetTokensFromCompressedContainer(CompressedContainer refCompressed)
        {
            var refTokens = from c in refCompressed.CompressedChunks
                            from s in ((CompressedChunkData)c.ChunkData).TokenSequences
                            from t in s.Tokens
                            select t;
            return refTokens;
        }
    }
}
