using System;
using System.Collections.Generic;
using System.IO;

namespace Kavod.Vba.Compression
{
    /// <summary>
    /// A LiteralToken is a copy of one byte, in uncompressed format, from the DecompressedBuffer 
    /// (section 2.4.1.1.2).
    /// </summary>
    /// <remarks></remarks>
    internal class LiteralToken : IToken, IEquatable<LiteralToken>
    {
        private readonly byte _data;

        internal LiteralToken(BinaryReader dataReader)
        {
            _data = BinaryUtilities.ReadExactly(dataReader, 1, "literal token")[0];
        }

        internal LiteralToken(byte data)
        {
            _data = data;
        }

        public void DecompressToken(List<byte> output)
        {
            ArgumentNullException.ThrowIfNull(output);

            output.Add(_data);
        }

        public byte[] SerializeData()
        {
            return [_data];
        }

        public void WriteTo(Stream stream)
        {
            ArgumentNullException.ThrowIfNull(stream);

            stream.WriteByte(_data);
        }

        public long Length => 1L;

        public int SerializedSize => 1;

        #region IEquatable
        public static bool operator !=(LiteralToken? first, LiteralToken? second)
        {
            return !(first == second);
        }

        public static bool operator ==(LiteralToken? first, LiteralToken? second)
        {
            return Equals(first, second);
        }

        public override bool Equals(object? obj)
        {
            return Equals(obj as LiteralToken);
        }

        public bool Equals(IToken? other)
        {
            return Equals(other as LiteralToken);
        }

        public bool Equals(LiteralToken? other)
        {
            if (ReferenceEquals(other, null))
            {
                return false;
            }

            return other._data == _data;
        }

        public override int GetHashCode()
        {
            return _data;
        }
        #endregion
    }
}
