using System;

namespace Kavod.Vba.Compression.Tests
{
    public class TestCopyToken
    {
        [Test]
        public async Task GivenRangeOfPositionOffsetAndLengthPackingThenunPackingDataProducesTheOriginalParameters()
        {
            const int increment = 5;

            for (var position = 2; position < 4096; position += increment)
            {
                for (UInt16 offset = 1; offset < position; offset = (ushort)(offset + increment))
                {
                    var result = CopyToken.CopyTokenHelp(position);

                    for (UInt16 length = 3; length <= result.MaximumLength; length = (ushort)(length + increment))
                    {
                        var tokenData = CopyToken.Pack(position, offset, length);

                        CopyToken.UnPack(tokenData, position, out var actualOffset, out var actualLength);

                        await Assert.That(actualOffset).IsEqualTo(offset);
                        await Assert.That(actualLength).IsEqualTo(length);
                    }
                }
            }
        }

        //  Position        #bits       Max Len         #bits
        //                  Len                         Offset
        //  ==================================================
        //  1 - 16          12          4098            4
        //  17 - 32         11          2050            5
        //  33 - 64         10          1026            6
        //  65 - 128        9           514             7
        //  129 - 256       8           258             8
        //  257 - 512       7           130             9
        //  513 - 1024      6           66              10
        //  1025 - 2048     5           34              11
        //  2049 - 4096     4           18              12
        [Test]
        [Arguments(1, 12, 4098, 4)]
        [Arguments(16, 12, 4098, 4)]
        [Arguments(17, 11, 2050, 5)]
        [Arguments(32, 11, 2050, 5)]
        [Arguments(33, 10, 1026, 6)]
        [Arguments(64, 10, 1026, 6)]
        [Arguments(65, 9, 514, 7)]
        [Arguments(128, 9, 514, 7)]
        [Arguments(129, 8, 258, 8)]
        [Arguments(256, 8, 258, 8)]
        [Arguments(257, 7, 130, 9)]
        [Arguments(512, 7, 130, 9)]
        [Arguments(513, 6, 66, 10)]
        [Arguments(1024, 6, 66, 10)]
        [Arguments(1025, 5, 34, 11)]
        [Arguments(2048, 5, 34, 11)]
        [Arguments(2049, 4, 18, 12)]
        [Arguments(4096, 4, 18, 12)]
        public async Task TestTokenHelp(int position, int expectedLengthBitCount, int expectedMaxLength, int expectedOffsetBitCount)
        {
            var result = CopyToken.CopyTokenHelp(position);

            await Assert.That((int)result.LengthBitCount).IsEqualTo(expectedLengthBitCount);
            await Assert.That((int)result.MaximumLength).IsEqualTo(expectedMaxLength);
            await Assert.That((int)result.BitCount).IsEqualTo(expectedOffsetBitCount);
        }
    }
}
