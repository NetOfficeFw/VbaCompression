using System;
using System.Collections.Generic;
using System.IO;

namespace Kavod.Vba.Compression
{
    /// <summary>
    /// A DecompressedChunk is a resizable array of bytes in the DecompressedBuffer 
    /// (section 2.4.1.1.2). The byte array is the data from a CompressedChunk (section 2.4.1.1.4) in 
    /// uncompressed format.
    /// </summary>
    /// <remarks></remarks>
    internal class DecompressedChunk
    {
        internal DecompressedChunk(CompressedChunk compressedChunk)
        {
            ArgumentNullException.ThrowIfNull(compressedChunk);

            if (compressedChunk.Header.IsCompressed)
            {
                var decompressedData = new List<byte>(Globals.MaxBytesPerChunk);
                var tokens = ((CompressedChunkData)compressedChunk.ChunkData).TokenSequences;
                foreach (var sequence in tokens)
                {
                    sequence.Tokens.DecompressTokenSequence(decompressedData);
                    if (decompressedData.Count > Globals.MaxBytesPerChunk)
                    {
                        throw new InvalidDataException($"Decompressed chunk exceeded {Globals.MaxBytesPerChunk} bytes.");
                    }
                }

                Data = decompressedData.ToArray();
            }
            else
            {
                Data = compressedChunk.ChunkData.SerializeData();
            }
        }

        internal DecompressedChunk(BinaryReader reader)
        {
            ArgumentNullException.ThrowIfNull(reader);

            var bytesToRead = reader.BaseStream.Length - reader.BaseStream.Position;

            if (bytesToRead > Globals.MaxBytesPerChunk)
                bytesToRead = Globals.MaxBytesPerChunk;

            Data = BinaryUtilities.ReadExactly(reader, (int)bytesToRead, "decompressed chunk");
        }

        internal byte[] Data { get; }
    }
}
