using System;
using System.IO;

namespace Kavod.Vba.Compression
{
    internal class RawChunk : IChunkData
    {
        private readonly byte[] _data;

        public RawChunk(byte[] data)
        {
            ArgumentNullException.ThrowIfNull(data);

            _data = data;
        }

        public byte[] SerializeData()
        {
            return _data;
        }

        public void WriteTo(Stream stream)
        {
            ArgumentNullException.ThrowIfNull(stream);

            stream.Write(_data);
        }

        public int Size => _data.Length;
    }
}
