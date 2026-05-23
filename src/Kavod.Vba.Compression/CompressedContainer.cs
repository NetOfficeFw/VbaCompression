using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Kavod.Vba.Compression
{
    /// <summary>
    /// A CompressedContainer is an array of bytes holding the compressed data. The Decompression 
    /// algorithm (section 2.4.1.3.1) processes a CompressedContainer to populate a DecompressedBuffer. 
    /// The Compression algorithm (section 2.4.1.3.6) processes a DecompressedBuffer to produce a 
    /// CompressedContainer.  A CompressedContainer MUST be the last array of bytes in a stream (1). 
    /// On read, the end of stream (1) indicator determines when the entire CompressedContainer has 
    /// been read.  The CompressedContainer is a SignatureByte followed by array of CompressedChunk 
    /// (section 2.4.1.1.4) structures.
    /// </summary>
    /// <remarks></remarks>
    internal class CompressedContainer
    {
        private const byte SignatureByteSig = 0x1;

        private readonly List<CompressedChunk> _compressedChunks = [];

        internal CompressedContainer(byte[] compressedData)
        {
            ArgumentNullException.ThrowIfNull(compressedData);

            if (compressedData.Length == 0)
            {
                throw new InvalidDataException("Compressed container is empty.");
            }

            using var reader = new BinaryReader(new MemoryStream(compressedData, writable: false));

            if (reader.ReadByte() != SignatureByteSig)
            {
                throw new InvalidDataException("Invalid compressed container signature byte.");
            }

            while (reader.BaseStream.Position < reader.BaseStream.Length)
            {
                _compressedChunks.Add(new CompressedChunk(reader));
            }
        }

        internal CompressedContainer(DecompressedBuffer buffer)
        {
            ArgumentNullException.ThrowIfNull(buffer);

            foreach (var chunk in buffer.DecompressedChunks)
            {
                _compressedChunks.Add(new CompressedChunk(chunk));
            }
        }

        internal IEnumerable<CompressedChunk> CompressedChunks => _compressedChunks;

        internal byte[] SerializeData()
        {
            using var stream = new MemoryStream(1 + _compressedChunks.Sum(c => c.SerializedSize));
            stream.WriteByte(SignatureByteSig);

            foreach (var chunk in CompressedChunks)
            {
                chunk.WriteTo(stream);
            }

            return stream.ToArray();
        }
    }
}
