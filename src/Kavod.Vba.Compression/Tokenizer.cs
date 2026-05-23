using System;
using System.Collections.Generic;

namespace Kavod.Vba.Compression
{
    internal static class Tokenizer
    {
        internal static IEnumerable<TokenSequence> ToTokenSequences(this IEnumerable<IToken> tokens)
        {
            ArgumentNullException.ThrowIfNull(tokens);

            var accumulatedTokens = new List<IToken>(8);
            foreach (var token in tokens)
            {
                if (accumulatedTokens.Count == 8)
                {
                    yield return new TokenSequence(accumulatedTokens);
                    accumulatedTokens.Clear();
                }

                accumulatedTokens.Add(token);
            }

            if (accumulatedTokens.Count != 0)
            {
                yield return new TokenSequence(accumulatedTokens);
            }
        }

        internal static void DecompressTokenSequence(this IEnumerable<IToken> tokens, List<byte> output)
        {
            ArgumentNullException.ThrowIfNull(tokens);
            ArgumentNullException.ThrowIfNull(output);

            foreach (var token in tokens)
            {
                token.DecompressToken(output);
            }
        }

        internal static IEnumerable<IToken> TokenizeUncompressedData(byte[] uncompressedData)
        {
            ArgumentNullException.ThrowIfNull(uncompressedData);

            var copyTokens = GetSpecificationCopyTokens(uncompressedData);
            foreach (var token in WeaveTokens(copyTokens, uncompressedData))
            {
                yield return token;
            }
        }

        private static IEnumerable<CopyToken> GetSpecificationCopyTokens(byte[] uncompressedData)
        {
            var position = 0L;
            while (position < uncompressedData.Length)
            {
                Match(uncompressedData, position, out var offset, out var length);

                if (length > 0)
                {
                    yield return new CopyToken(position, offset, length);
                    position += length;
                }
                else
                {
                    position++;
                }
            }
        }

        private static IEnumerable<IToken> WeaveTokens(IEnumerable<CopyToken> copyTokens, byte[] uncompressedData)
        {
            var position = 0L;
            foreach (var currentCopyToken in copyTokens)
            {
                while (position < currentCopyToken.Position)
                {
                    yield return new LiteralToken(uncompressedData[position]);
                    position++;
                }

                yield return currentCopyToken;
                position += currentCopyToken.Length;
            }

            while (position < uncompressedData.Length)
            {
                yield return new LiteralToken(uncompressedData[position]);
                position++;
            }
        }

        internal static void Match(byte[] uncompressedData, long position, out UInt16 matchedOffset, out UInt16 matchedLength)
        {
            ArgumentNullException.ThrowIfNull(uncompressedData);

            if (position < 0 || position > uncompressedData.Length)
            {
                throw new ArgumentOutOfRangeException(nameof(position), position, "Position must be inside the decompressed chunk.");
            }

            var decompressedCurrent = position;
            var decompressedEnd = uncompressedData.Length;
            const long decompressedChunkStart = 0;
            var maximumLength = CopyToken.CopyTokenHelp(decompressedCurrent).MaximumLength;

            var candidate = decompressedCurrent - 1L;
            var bestLength = 0L;
            var bestCandidate = 0L;

            while (candidate >= decompressedChunkStart)
            {
                var c = candidate;
                var d = decompressedCurrent;
                var length = 0;

                while (d < decompressedEnd
                       && length < maximumLength
                       && uncompressedData[d] == uncompressedData[c])
                {
                    length++;
                    c++;
                    d++;
                }

                if (length > bestLength)
                {
                    bestLength = length;
                    bestCandidate = candidate;

                    if (bestLength == maximumLength)
                    {
                        break;
                    }
                }

                candidate--;
            }

            if (bestLength >= 3)
            {
                matchedLength = (UInt16)bestLength;
                matchedOffset = (UInt16)(decompressedCurrent - bestCandidate);
            }
            else
            {
                matchedLength = 0;
                matchedOffset = 0;
            }
        }
    }
}
