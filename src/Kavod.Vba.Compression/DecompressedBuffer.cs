using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Kavod.Vba.Compression
{
    /// <summary>
    /// The DecompressedBuffer is a resizable array of bytes that contains the same data as the 
    /// CompressedContainer (section 2.4.1.1.1), but the data is in an uncompressed format.
    /// </summary>
    /// <remarks></remarks>
    internal class DecompressedBuffer
    {
        internal DecompressedBuffer(byte[] uncompressedData)
        {
            ArgumentNullException.ThrowIfNull(uncompressedData);

            using var reader = new BinaryReader(new MemoryStream(uncompressedData, writable: false));
            while (reader.BaseStream.Position < reader.BaseStream.Length)
            {
                var chunk = new DecompressedChunk(reader);
                DecompressedChunks.Add(chunk);
            }
        }

        internal DecompressedBuffer(CompressedContainer container)
        {
            ArgumentNullException.ThrowIfNull(container);

            foreach (var chunk in container.CompressedChunks)
            {
                DecompressedChunks.Add(new DecompressedChunk(chunk));
            }
        }

        internal List<DecompressedChunk> DecompressedChunks { get; } = new List<DecompressedChunk>();

        internal byte[] Data
        {
            get
            {
                using var stream = new MemoryStream(DecompressedChunks.Sum(c => c.Data.Length));
                foreach (var chunk in DecompressedChunks)
                {
                    stream.Write(chunk.Data);
                }

                return stream.ToArray();
            }
        }
    }
}
