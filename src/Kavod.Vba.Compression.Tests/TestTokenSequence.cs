using System;
using Xunit;

namespace Kavod.Vba.Compression.Tests
{
    public class TestTokenSequence
    {
        const int TokenIndexToCheck = 3;

        [Fact]
        public void TestMatchMethod()
        {
            var bytes = new byte[] { 1, 1, 1, 2, 1, 1, 1, 2, 1, 2 };

            Tokenizer.Match(bytes, 4, out var offset, out var length);

            Assert.Equal(Convert.ToUInt16(4), offset);
            Assert.Equal(Convert.ToUInt16(5), length);
        }
    }
}
