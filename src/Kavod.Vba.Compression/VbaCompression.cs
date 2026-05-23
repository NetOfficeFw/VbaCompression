using System;

namespace Kavod.Vba.Compression
{
    public static class VbaCompression
    {
        public static byte[] Compress(byte[] data)
        {
            ArgumentNullException.ThrowIfNull(data);

            var buffer = new DecompressedBuffer(data);
            var container = new CompressedContainer(buffer);
            return container.SerializeData();
        }

        public static byte[] Decompress(byte[] data)
        {
            ArgumentNullException.ThrowIfNull(data);

            var container = new CompressedContainer(data);
            var buffer = new DecompressedBuffer(container);
            return buffer.Data;
        }
    }
}
