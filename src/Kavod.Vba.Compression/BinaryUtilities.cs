using System;
using System.Buffers.Binary;
using System.IO;

namespace Kavod.Vba.Compression
{
    internal static class BinaryUtilities
    {
        internal static byte[] ReadExactly(BinaryReader reader, int count, string fieldName)
        {
            ArgumentNullException.ThrowIfNull(reader);

            if (count < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(count), count, "Byte count must not be negative.");
            }

            var data = new byte[count];
            try
            {
                reader.BaseStream.ReadExactly(data);
            }
            catch (EndOfStreamException ex)
            {
                throw new InvalidDataException($"Unexpected end of data while reading {fieldName}.", ex);
            }

            return data;
        }

        internal static ushort ReadUInt16LittleEndian(BinaryReader reader, string fieldName)
        {
            ArgumentNullException.ThrowIfNull(reader);

            Span<byte> buffer = stackalloc byte[sizeof(ushort)];
            try
            {
                reader.BaseStream.ReadExactly(buffer);
            }
            catch (EndOfStreamException ex)
            {
                throw new InvalidDataException($"Unexpected end of data while reading {fieldName}.", ex);
            }

            return BinaryPrimitives.ReadUInt16LittleEndian(buffer);
        }

        internal static byte[] GetUInt16LittleEndianBytes(ushort value)
        {
            var buffer = new byte[sizeof(ushort)];
            BinaryPrimitives.WriteUInt16LittleEndian(buffer, value);
            return buffer;
        }

        internal static void WriteUInt16LittleEndian(Stream stream, ushort value)
        {
            ArgumentNullException.ThrowIfNull(stream);

            Span<byte> buffer = stackalloc byte[sizeof(ushort)];
            BinaryPrimitives.WriteUInt16LittleEndian(buffer, value);
            stream.Write(buffer);
        }
    }
}
