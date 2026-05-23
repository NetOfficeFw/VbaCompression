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
            MatchFinder? matchFinder = null;
            var position = 0;
            var indexedUntil = 0;

            while (position < uncompressedData.Length)
            {
                UInt16 offset;
                UInt16 length;

                if (position == 0)
                {
                    offset = 0;
                    length = 0;
                }
                else if (!TryMatchRun(uncompressedData, position, out offset, out length))
                {
                    matchFinder ??= new MatchFinder(uncompressedData);
                    matchFinder.AddPositions(indexedUntil, position);
                    indexedUntil = position;

                    matchFinder.Match(position, out offset, out length);
                }

                var nextPosition = position + (length > 0 ? length : 1);
                if (matchFinder is not null)
                {
                    matchFinder.AddPositions(indexedUntil, nextPosition);
                    indexedUntil = nextPosition;
                }

                if (length > 0)
                {
                    yield return new CopyToken(position, offset, length);
                }

                position = nextPosition;
            }
        }

        private static bool TryMatchRun(byte[] uncompressedData, int position, out UInt16 matchedOffset, out UInt16 matchedLength)
        {
            if (position == 0
                || position > uncompressedData.Length - 3
                || uncompressedData[position] != uncompressedData[position - 1])
            {
                matchedOffset = 0;
                matchedLength = 0;
                return false;
            }

            var maximumLength = CopyToken.CopyTokenHelp(position).MaximumLength;
            var currentByte = uncompressedData[position];
            var length = 0;

            while (length < maximumLength
                   && position + length < uncompressedData.Length
                   && uncompressedData[position + length] == currentByte)
            {
                length++;
            }

            if (length < 3)
            {
                matchedOffset = 0;
                matchedLength = 0;
                return false;
            }

            matchedOffset = 1;
            matchedLength = (UInt16)length;
            return true;
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

            MatchCore(uncompressedData, position, out matchedOffset, out matchedLength);
        }

        private static void MatchCore(byte[] uncompressedData, long position, out UInt16 matchedOffset, out UInt16 matchedLength)
        {
            var decompressedCurrent = position;
            var decompressedEnd = uncompressedData.Length;
            const long decompressedChunkStart = 0;
            var maximumLength = 0;

            var candidate = decompressedCurrent - 1L;
            var bestLength = 0L;
            var bestCandidate = 0L;

            while (candidate >= decompressedChunkStart)
            {
                var c = candidate;
                var d = decompressedCurrent;
                var length = 0;

                while (d < decompressedEnd
                       && uncompressedData[d] == uncompressedData[c])
                {
                    length++;
                    c++;
                    d++;

                    if (length == 3)
                        break;
                }

                if (length == 3)
                {
                    if (maximumLength == 0)
                    {
                        maximumLength = CopyToken.CopyTokenHelp(decompressedCurrent).MaximumLength;
                    }

                    while (length < maximumLength
                           && d < decompressedEnd
                           && uncompressedData[d] == uncompressedData[c])
                    {
                        length++;
                        c++;
                        d++;
                    }
                }

                if (length > bestLength)
                {
                    bestLength = length;
                    bestCandidate = candidate;

                    if (maximumLength != 0 && bestLength == maximumLength)
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

        private sealed class MatchFinder
        {
            private readonly byte[] _data;
            private readonly Dictionary<int, short> _heads = new Dictionary<int, short>();
            private readonly short[] _previous;

            internal MatchFinder(byte[] data)
            {
                _data = data;
                _previous = new short[data.Length];
                Array.Fill(_previous, (short)-1);
            }

            internal void AddPositions(int start, int end)
            {
                var lastCandidatePosition = _data.Length - 3;
                if (start > lastCandidatePosition)
                {
                    return;
                }

                end = Math.Min(end, lastCandidatePosition + 1);
                for (var position = start; position < end; position++)
                {
                    var key = GetKey(position);
                    if (_heads.TryGetValue(key, out var previous))
                    {
                        _previous[position] = previous;
                    }

                    _heads[key] = (short)position;
                }
            }

            internal void Match(int position, out UInt16 matchedOffset, out UInt16 matchedLength)
            {
                if (position > _data.Length - 3 || !_heads.TryGetValue(GetKey(position), out var candidateValue))
                {
                    matchedLength = 0;
                    matchedOffset = 0;
                    return;
                }

                var candidate = (int)candidateValue;
                var maximumLength = CopyToken.CopyTokenHelp(position).MaximumLength;
                var bestLength = 0;
                var bestCandidate = 0;

                while (candidate >= 0)
                {
                    var c = candidate;
                    var d = position;
                    var length = 0;

                    while (length < maximumLength
                           && d < _data.Length
                           && _data[d] == _data[c])
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

                    candidate = _previous[candidate];
                }

                if (bestLength >= 3)
                {
                    matchedLength = (UInt16)bestLength;
                    matchedOffset = (UInt16)(position - bestCandidate);
                }
                else
                {
                    matchedLength = 0;
                    matchedOffset = 0;
                }
            }

            private int GetKey(int position)
            {
                return (_data[position] << 16)
                       | (_data[position + 1] << 8)
                       | _data[position + 2];
            }
        }
    }
}
