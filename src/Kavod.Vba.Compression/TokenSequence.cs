using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Kavod.Vba.Compression
{
    /// <summary>
    /// A TokenSequence is a FlagByte followed by an array of Tokens. The number of Tokens in the final 
    /// TokenSequence MUST be greater than or equal to 1. The number of Tokens in the final 
    /// TokenSequence MUST less than or equal to eight. All other TokenSequences in the 
    /// CompressedChunkData MUST contain eight Tokens.
    /// </summary>
    /// <remarks></remarks>
    internal class TokenSequence
    {
        private byte _flagByte;
        private readonly List<IToken> _tokens = new List<IToken>();

        public TokenSequence(IEnumerable<IToken> enumerable) : this()
        {
            ArgumentNullException.ThrowIfNull(enumerable);

            _tokens.AddRange(enumerable);

            if (_tokens.Count is 0 or > 8)
            {
                throw new ArgumentException("A token sequence must contain between 1 and 8 tokens.", nameof(enumerable));
            }

            // set the flag byte.
            for (var i = 0; i < _tokens.Count; i++)
            {
                if (_tokens[i] is CopyToken)
                {
                    SetIsCopyToken(i);
                }
            }
        }

        private TokenSequence()
        { }

        internal long Length => Tokens.Sum(t => t.Length);

        internal int SerializedSize => 1 + Tokens.Sum(t => t.SerializedSize);

        internal IReadOnlyList<IToken> Tokens => _tokens;

        internal static TokenSequence GetFromCompressedData(BinaryReader reader, long position, long sequenceEndPosition)
        {
            ArgumentNullException.ThrowIfNull(reader);

            if (reader.BaseStream.Position >= sequenceEndPosition)
            {
                throw new InvalidDataException("Compressed token sequence is missing a flag byte.");
            }

            var sequence = new TokenSequence
            {
                _flagByte = reader.ReadByte()
            };

            for (var i = 0; i < 8 && reader.BaseStream.Position < sequenceEndPosition; i++)
            {
                if (sequence.GetIsCopyToken(i))
                {
                    if (sequenceEndPosition - reader.BaseStream.Position < sizeof(ushort))
                    {
                        throw new InvalidDataException("Compressed token sequence ended in the middle of a copy token.");
                    }

                    var token = new CopyToken(reader, position);
                    sequence._tokens.Add(token);
                    position += Convert.ToInt64(token.Length);
                }
                else
                {
                    sequence._tokens.Add(new LiteralToken(reader));
                    position += 1;
                }
            }

            if (sequence._tokens.Count == 0)
            {
                throw new InvalidDataException("A compressed token sequence must contain at least one token.");
            }

            return sequence;
        }

        private void SetIsCopyToken(int index)
        {
            _flagByte = (byte)(_flagByte | (1 << index));
        }

        private bool GetIsCopyToken(int index)
        {
            var compareByte = (byte)(1 << index);
            return (compareByte & _flagByte) != 0x0;
        }

        internal byte[] SerializeData()
        {
            using var stream = new MemoryStream(SerializedSize);
            WriteTo(stream);
            return stream.ToArray();
        }

        internal void WriteTo(Stream stream)
        {
            ArgumentNullException.ThrowIfNull(stream);

            stream.WriteByte(_flagByte);
            foreach (var token in Tokens)
            {
                token.WriteTo(stream);
            }
        }
    }
}
