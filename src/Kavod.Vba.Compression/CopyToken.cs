using System;
using System.Collections.Generic;
using System.IO;

namespace Kavod.Vba.Compression
{
    /// <summary>
    /// CopyToken is a two-byte record interpreted as an unsigned 16-bit integer in little-endian 
    /// order. A CopyToken is a compressed encoding of an array of bytes from a DecompressedChunk 
    /// (section 2.4.1.1.3). The byte array encoded by a CopyToken is a byte-for-byte copy of a byte 
    /// array elsewhere in the same DecompressedChunk, called a CopySequence (section 2.4.1.3.19).  
    /// 
    /// The starting location, in a DecompressedChunk, is determined by the Compressing a Token 
    /// (section 2.4.1.3.9) and the Decompressing a Token (section 2.4.1.3.5) algorithms. Packed into 
    /// the CopyToken is the Offset, the distance, in byte count, to the beginning of the CopySequence. 
    /// Also packed into the CopyToken is the Length, the number of bytes encoded in the CopyToken. 
    /// Length also specifies the count of bytes in the CopySequence. The values encoded in Offset and 
    /// Length are computed by the Matching (section 2.4.1.3.19.4) algorithm.
    /// </summary>
    /// <remarks></remarks>
    internal class CopyToken : IToken, IEquatable<CopyToken>
    {
        private readonly UInt16 _tokenOffset;
        private readonly UInt16 _tokenLength;

        /// <summary>
        /// Constructor used to create a CopyToken when compressing a DecompressedChunk.
        /// </summary>
        /// <param name="tokenPosition">
        /// The start position of the CopyToken decompressed data in the current DecompressedChunk.
        /// </param>
        /// <param name="tokenOffset">
        /// The offset in bytes from the start position in the current DecompressedChunk from which to 
        /// start copying.
        /// </param>
        /// <param name="tokenLength">The number of bytes to copy from the offset.</param>
        /// <remarks></remarks>

        internal CopyToken(long tokenPosition, UInt16 tokenOffset, UInt16 tokenLength)
        {
            if (tokenPosition < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(tokenPosition), tokenPosition, "Token position must not be negative.");
            }

            if (tokenOffset == 0)
            {
                throw new ArgumentOutOfRangeException(nameof(tokenOffset), tokenOffset, "Copy token offset must be at least 1.");
            }

            if (tokenLength < 3)
            {
                throw new ArgumentOutOfRangeException(nameof(tokenLength), tokenLength, "Copy token length must be at least 3.");
            }

            Position = tokenPosition;
            _tokenOffset = tokenOffset;
            _tokenLength = tokenLength;
        }

        /// <summary>
        /// Constructor used to create CopyToken instance when reading compressed token from a stream.
        /// </summary>
        /// <param name="dataReader">
        /// A BinaryReader object where the position is located at an encoded CopyToken.
        /// </param>
        /// <remarks></remarks>
        internal CopyToken(BinaryReader dataReader, long position)
        {
            Position = position;
            CopyToken.UnPack(BinaryUtilities.ReadUInt16LittleEndian(dataReader, "copy token"), Position, out _tokenOffset, out _tokenLength);
        }

        public long Length => _tokenLength;

        internal UInt16 Offset => _tokenOffset;

        internal long Position { get; }

        internal static UInt16 Pack(long position, UInt16 offset, UInt16 length)
        {
            // 2.4.1.3.19.3 Pack CopyToken
            var result = CopyTokenHelp(position);

            if (offset == 0)
                throw new ArgumentOutOfRangeException(nameof(offset), offset, "Copy token offset must be at least 1.");
            if (offset > position)
                throw new ArgumentOutOfRangeException(nameof(offset), offset, "Copy token offset must not exceed the token position.");
            if (length < 3)
                throw new ArgumentOutOfRangeException(nameof(length), length, "Copy token length must be at least 3.");
            if (length > result.MaximumLength)
                throw new ArgumentOutOfRangeException(nameof(length), length, $"Copy token length must not exceed {result.MaximumLength} at position {position}.");

            //SET temp1 TO Offset MINUS 1
            var temp1 = (UInt16)(offset - 1);

            //SET temp2 TO 16 MINUS BitCount
            var temp2 = (UInt16)(16 - result.BitCount);

            //SET temp3 TO Length MINUS 3
            var temp3 = (UInt16)(length - 3);

            //SET Token TO (temp1 LEFT SHIFT BY temp2) BITWISE OR temp3
            return (UInt16)((temp1 << temp2) | temp3);
        }

        public void DecompressToken(List<byte> output)
        {
            ArgumentNullException.ThrowIfNull(output);

            if (_tokenOffset > output.Count)
            {
                throw new InvalidDataException($"Copy token offset {_tokenOffset} exceeds decompressed byte count {output.Count}.");
            }

            var copySource = output.Count - _tokenOffset;
            for (var i = 0; i < _tokenLength; i++)
            {
                output.Add(output[copySource + i]);
            }
        }

        internal static void UnPack(UInt16 packedToken, long position, out UInt16 unpackedOffset, out UInt16 unpackedLength)
        {
            // CALL CopyToken Help (section 2.4.1.3.19.1) returning LengthMask, OffsetMask, and BitCount.
            var result = CopyToken.CopyTokenHelp(position);

            // SET Length TO (Token BITWISE AND LengthMask) PLUS 3.
            unpackedLength = (UInt16)((packedToken & result.LengthMask) + 3);

            // SET temp1 TO Token BITWISE AND OffsetMask.
            var temp1 = (UInt16)(packedToken & result.OffsetMask);

            // SET temp2 TO 16 MINUS BitCount.
            var temp2 = (UInt16)(16 - result.BitCount);

            // SET Offset TO (temp1 RIGHT SHIFT BY temp2) PLUS 1.
            unpackedOffset = (UInt16)((temp1 >> temp2) + 1);
        }

        /// <summary>
        /// CopyToken Help derived bit masks are used by the Unpack CopyToken (section 2.4.1.3.19.2) 
        /// and the Pack CopyToken (section 2.4.1.3.19.3) algorithms. CopyToken Help also derives the 
        /// maximum length for a CopySequence (section 2.4.1.3.19) which is used by the Matching 
        /// algorithm (section 2.4.1.3.19.4).
        /// The pseudocode uses the state variables described in State Variables (section 2.4.1.2): 
        /// DecompressedCurrent and DecompressedChunkStart.
        /// </summary>
        internal static CopyTokenHelpResult CopyTokenHelp(long difference)
        {
            if (difference < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(difference), difference, "Position difference must not be negative.");
            }

            var result = new CopyTokenHelpResult();

            // SET BitCount TO the smallest integer that is GREATER THAN OR EQUAL TO LOGARITHM base 2 
            // of difference
            result.BitCount = 0;
            while ((1 << result.BitCount) < difference)
            {
                result.BitCount += 1;
            }

            // The number of bits used to encode Length MUST be greater than or equal to four. The 
            // number of bits used to encode Length MUST be less than or equal to 12
            // SET BitCount TO the maximum of BitCount and 4
            if (result.BitCount < 4)
                result.BitCount = 4;
            if (result.BitCount > 12)
                throw new InvalidDataException($"Copy token position difference {difference} requires too many offset bits.");

            // SET LengthMask TO 0xFFFF RIGHT SHIFT BY BitCount
            result.LengthMask = (UInt16)(0xffff >> result.BitCount);

            // SET OffsetMask TO BITWISE NOT LengthMask
            result.OffsetMask = (UInt16)(~result.LengthMask);

            // SET MaximumLength TO (0xFFFF RIGHT SHIFT BY BitCount) PLUS 3
            result.MaximumLength = (UInt16)((0xffff >> result.BitCount) + 3);

            return result;
        }

        public byte[] SerializeData()
        {
            var packedData = Pack(Position, _tokenOffset, _tokenLength);
            return BinaryUtilities.GetUInt16LittleEndianBytes(packedData);
        }

        public void WriteTo(Stream stream)
        {
            ArgumentNullException.ThrowIfNull(stream);

            BinaryUtilities.WriteUInt16LittleEndian(stream, Pack(Position, _tokenOffset, _tokenLength));
        }

        public int SerializedSize => sizeof(ushort);

        #region Nested Classes

        internal struct CopyTokenHelpResult
        {
            internal UInt16 LengthMask { get; set; }
            internal UInt16 OffsetMask { get; set; }
            internal UInt16 BitCount { get; set; }  // offset bit count.
            internal UInt16 MaximumLength { get; set; }
            internal UInt16 LengthBitCount => (UInt16)(16 - BitCount);
        }

        #endregion

        #region IEquatable
        public static bool operator !=(CopyToken? first, CopyToken? second)
        {
            return !(first == second);
        }

        public static bool operator ==(CopyToken? first, CopyToken? second)
        {
            return Equals(first, second);
        }

        public override bool Equals(object? obj)
        {
            return Equals(obj as CopyToken);
        }

        public bool Equals(IToken? other)
        {
            return Equals(other as CopyToken);
        }

        public bool Equals(CopyToken? other)
        {
            if (ReferenceEquals(other, null))
            {
                return false;
            }
            return other.Position == Position
                   && other.Length == Length
                   && other.Offset == Offset;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Position, Length, Offset);
        }
        #endregion
    }
}
