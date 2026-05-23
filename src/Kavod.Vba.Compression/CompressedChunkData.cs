using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Kavod.Vba.Compression
{
    /// <summary>
    /// If CompressedChunkHeader.CompressedChunkFlag (section 2.4.1.1.5) is 0b0, CompressedChunkData 
    /// contains an array of CompressedChunkHeader.CompressedChunkSize elements plus 3 bytes of 
    /// uncompressed data.  If CompressedChunkHeader CompressedChunkFlag is 0b1, CompressedChunkData 
    /// contains an array of TokenSequence (section 2.4.1.1.7) elements.
    /// </summary>
    /// <remarks></remarks>
    internal class CompressedChunkData : IChunkData
    {
        private readonly List<TokenSequence> _tokensequences = new List<TokenSequence>();

        internal CompressedChunkData(DecompressedChunk chunk)
        {
            ArgumentNullException.ThrowIfNull(chunk);

            var tokens = Tokenizer.TokenizeUncompressedData(chunk.Data);
            _tokensequences.AddRange(tokens.ToTokenSequences());
        }

        internal CompressedChunkData(BinaryReader dataReader, UInt16 compressedChunkDataSize)
        {
            var data = BinaryUtilities.ReadExactly(dataReader, compressedChunkDataSize, "compressed chunk data");

            using var reader = new BinaryReader(new MemoryStream(data));
            var position = 0;
            while (reader.BaseStream.Position < reader.BaseStream.Length)
            {
                var sequence = TokenSequence.GetFromCompressedData(reader, position, reader.BaseStream.Length);
                _tokensequences.Add(sequence);
                position += (int)sequence.Tokens.Sum(t => t.Length);
            }
        }

        internal IEnumerable<TokenSequence> TokenSequences => _tokensequences;

        public byte[] SerializeData()
        {
            using var stream = new MemoryStream(Size);
            WriteTo(stream);
            return stream.ToArray();
        }

        public void WriteTo(Stream stream)
        {
            ArgumentNullException.ThrowIfNull(stream);

            foreach (var sequence in _tokensequences)
            {
                sequence.WriteTo(stream);
            }
        }

        public int Size => _tokensequences.Sum(t => t.SerializedSize);
    }
}
