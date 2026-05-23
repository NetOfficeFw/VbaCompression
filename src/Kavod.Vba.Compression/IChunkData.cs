using System.IO;

namespace Kavod.Vba.Compression
{
    internal interface IChunkData
    {
        byte[] SerializeData();

        void WriteTo(Stream stream);

        int Size { get; }
    }
}
