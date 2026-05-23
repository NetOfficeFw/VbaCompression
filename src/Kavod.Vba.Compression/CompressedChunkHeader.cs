using System;
using System.IO;

namespace Kavod.Vba.Compression
{
    /// <summary>
    /// A CompressedChunkHeader is the first record in a CompressedChunk (section 2.4.1.1.4). A 
    /// CompressedChunkHeader specifies the size of the entire CompressedChunk and the data encoding 
    /// format in CompressedChunk.CompressedData. CompressedChunkHeader information is used by the 
    /// Decompressing a CompressedChunk (section 2.4.1.3.2) and Compressing a DecompressedChunk 
    /// (section 2.4.1.3.7) algorithms.
    /// </summary>
    /// <remarks></remarks>
    internal class CompressedChunkHeader
    {
        private const ushort CompressedChunkFlag = 0x8000;
        private const ushort ChunkSignature = 0x3000;
        private const ushort ChunkSignatureMask = 0x7000;
        private const ushort ChunkSizeMask = 0x0fff;

        internal CompressedChunkHeader(IChunkData chunkData)
        {
            ArgumentNullException.ThrowIfNull(chunkData);

            IsCompressed = chunkData is CompressedChunkData;
            CompressedChunkSize = (ushort)(chunkData.Size + 2);
            ValidateChunkSizeAndCompressedFlag();
        }

        internal CompressedChunkHeader(UInt16 header)
        {
            DecodeHeader(header);
        }

        internal CompressedChunkHeader(BinaryReader dataReader)
        {
            ArgumentNullException.ThrowIfNull(dataReader);

            var header = BinaryUtilities.ReadUInt16LittleEndian(dataReader, "compressed chunk header");
            DecodeHeader(header);
        }

        private void DecodeHeader(UInt16 header)
        {
            if ((header & ChunkSignatureMask) != ChunkSignature)
            {
                throw new InvalidDataException($"Invalid compressed chunk header signature: 0x{header:X4}.");
            }

            IsCompressed = (header & CompressedChunkFlag) != 0;

            // 2.4.1.3.12 Extract CompressedChunkSize
            // SET temp TO Header BITWISE AND 0x0FFF
            // SET Size TO temp PLUS 3
            CompressedChunkSize = (UInt16)((header & ChunkSizeMask) + 3);

            ValidateChunkSizeAndCompressedFlag();
        }

        internal bool IsCompressed { get; private set; }

        internal UInt16 CompressedChunkSize { get; private set; }

        internal UInt16 CompressedChunkDataSize => (UInt16)(CompressedChunkSize - 2);

        internal byte[] SerializeData()
        {
            ValidateChunkSizeAndCompressedFlag();

            return BinaryUtilities.GetUInt16LittleEndianBytes(GetHeaderValue());
        }

        internal void WriteTo(Stream stream)
        {
            ArgumentNullException.ThrowIfNull(stream);

            ValidateChunkSizeAndCompressedFlag();
            BinaryUtilities.WriteUInt16LittleEndian(stream, GetHeaderValue());
        }

        private UInt16 GetHeaderValue()
        {
            if (IsCompressed)
            {
                return (UInt16)(CompressedChunkFlag | ChunkSignature | (CompressedChunkSize - 3));
            }

            return (UInt16)(ChunkSignature | (CompressedChunkSize - 3));
        }

        private void ValidateChunkSizeAndCompressedFlag()
        {
            if (CompressedChunkSize < 3)
            {
                throw new InvalidDataException($"Compressed chunk size {CompressedChunkSize} is below the minimum size 3.");
            }

            if (IsCompressed
                && CompressedChunkSize > 4098)
            {
                throw new InvalidDataException($"Compressed chunk size {CompressedChunkSize} exceeds the maximum size 4098.");
            }
            if (!IsCompressed
                && CompressedChunkSize != 4098)
            {
                throw new InvalidDataException($"Raw chunk size must be 4098 bytes, but was {CompressedChunkSize}.");
            }
        }
    }
}
