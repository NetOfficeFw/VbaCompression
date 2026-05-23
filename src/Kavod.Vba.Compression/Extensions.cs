using System;
using System.Diagnostics;
using System.Text;

namespace Kavod.Vba.Compression
{
    internal static class Extensions
    {
        [DebuggerStepThrough]
        internal static byte[] ToMcbsBytes(this string textToConvert, UInt16 codePage)
        {
            ArgumentNullException.ThrowIfNull(textToConvert);

#if NET
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
#endif
            return Encoding.GetEncoding(codePage).GetBytes(textToConvert);
        }

        internal static byte[] StringToByteArray(string hex)
        {
            ArgumentNullException.ThrowIfNull(hex);

            return Convert.FromHexString(hex);
        }
    }
}
