using System;
using System.IO;

namespace Kavod.Vba.Compression
{
    /// <summary>
    /// A CompressedChunk is a record that encodes all data from a DecompressedChunk (section 
    /// 2.4.1.1.3) in compressed format. A CompressedChunk has two parts: a CompressedChunkHeader 
    /// (section 2.4.1.1.5) followed by a CompressedChunkData (section 2.4.1.1.6). The number of bytes 
    /// in a CompressedChunk MUST be greater than or equal to 3. The number of bytes in a 
    /// CompressedChunk MUST be less than or equal to 4098.
    /// </summary>
    /// <remarks></remarks>
    internal class CompressedChunk
    {
        internal CompressedChunk(DecompressedChunk decompressedChunk)
        {
            ArgumentNullException.ThrowIfNull(decompressedChunk);

            ChunkData = new CompressedChunkData(decompressedChunk);
            if (ChunkData.Size >= Globals.MaxBytesPerChunk)
            {
                ChunkData = new RawChunk(decompressedChunk.Data);
            }
            Header = new CompressedChunkHeader(ChunkData);
        }

        internal CompressedChunk(BinaryReader dataReader)
        {
            ArgumentNullException.ThrowIfNull(dataReader);

            Header = new CompressedChunkHeader(dataReader);
            if (Header.IsCompressed)
            {
                ChunkData = new CompressedChunkData(dataReader, Header.CompressedChunkDataSize);
            }
            else
            {
                ChunkData = new RawChunk(BinaryUtilities.ReadExactly(dataReader, Header.CompressedChunkDataSize, "raw chunk data"));
            }
        }

        internal CompressedChunkHeader Header { get; }

        internal IChunkData ChunkData { get; }

        internal byte[] SerializeData()
        {
            using var stream = new MemoryStream(SerializedSize);
            WriteTo(stream);
            return stream.ToArray();
        }

        internal void WriteTo(Stream stream)
        {
            ArgumentNullException.ThrowIfNull(stream);

            Header.WriteTo(stream);
            ChunkData.WriteTo(stream);

            if (!Header.IsCompressed)
            {
                var paddingLength = SerializedSize - Globals.NumberOfChunkHeaderBytes - ChunkData.Size;
                if (paddingLength > 0)
                {
                    stream.Write(new byte[paddingLength]);
                }
            }
        }

        internal int SerializedSize => Header.IsCompressed
            ? Globals.NumberOfChunkHeaderBytes + ChunkData.Size
            : Globals.NumberOfChunkHeaderBytes + Globals.MaxBytesPerChunk;
    }
}
