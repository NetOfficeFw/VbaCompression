using System;
using System.Collections.Generic;
using System.IO;

namespace Kavod.Vba.Compression
{
    internal interface IToken : IEquatable<IToken>
    {
        void DecompressToken(List<byte> output);

        byte[] SerializeData();

        void WriteTo(Stream stream);

        long Length { get; }

        int SerializedSize { get; }
    }
}
