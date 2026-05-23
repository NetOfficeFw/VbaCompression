using System.Text;

namespace Kavod.Vba.Compression.Benchmarks;

internal static class BenchmarkInputs
{
    private const int InputSize = 256 * 1024;

    public static IReadOnlyList<string> InputNames { get; } =
    [
        "RepeatedBytes",
        "VbaLikeSource",
        "MixedPattern",
        "LowCompressibility"
    ];

    public static byte[] Create(string inputName)
    {
        return inputName switch
        {
            "RepeatedBytes" => CreateRepeatedBytes(InputSize),
            "VbaLikeSource" => CreateVbaLikeSource(InputSize),
            "MixedPattern" => CreateMixedPattern(InputSize),
            "LowCompressibility" => CreateLowCompressibility(InputSize),
            _ => throw new ArgumentOutOfRangeException(nameof(inputName), inputName, "Unknown benchmark input.")
        };
    }

    private static byte[] CreateRepeatedBytes(int size)
    {
        return Enumerable.Repeat((byte)'A', size).ToArray();
    }

    private static byte[] CreateVbaLikeSource(int size)
    {
        const string moduleText = """
Option Explicit

Private Sub Worksheet_Change(ByVal Target As Range)
    If Target.CountLarge > 1 Then Exit Sub
    If Target.Column = 1 Then
        Application.EnableEvents = False
        Target.Offset(0, 1).Value = Now
        Application.EnableEvents = True
    End If
End Sub

Public Function NormalizeName(ByVal value As String) As String
    NormalizeName = Trim$(Replace(value, vbTab, " "))
End Function

""";

        return RepeatUtf8(moduleText, size);
    }

    private static byte[] CreateMixedPattern(int size)
    {
        var buffer = new byte[size];
        var words = Encoding.ASCII.GetBytes("Function,Sub,Dim,Range,Cells,Value,Module,Class,End,If,Then,Else");
        var random = CreateLowCompressibility(size / 4);

        for (var i = 0; i < buffer.Length; i++)
        {
            buffer[i] = i % 4 == 0
                ? random[i / 4]
                : words[i % words.Length];
        }

        return buffer;
    }

    private static byte[] CreateLowCompressibility(int size)
    {
        var buffer = new byte[size];
        var state = 0x6D2B79F5u;

        for (var i = 0; i < buffer.Length; i++)
        {
            state ^= state << 13;
            state ^= state >> 17;
            state ^= state << 5;
            buffer[i] = (byte)state;
        }

        return buffer;
    }

    private static byte[] RepeatUtf8(string text, int size)
    {
        var source = Encoding.UTF8.GetBytes(text);
        var buffer = new byte[size];

        for (var offset = 0; offset < buffer.Length; offset += source.Length)
        {
            var count = Math.Min(source.Length, buffer.Length - offset);
            Array.Copy(source, 0, buffer, offset, count);
        }

        return buffer;
    }
}
