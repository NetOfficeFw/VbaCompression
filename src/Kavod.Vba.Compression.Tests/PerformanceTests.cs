using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace Kavod.Vba.Compression.Tests
{
    public class PerformanceTests
    {
        [Test]
        [Arguments(PerformanceInputKind.RepeatedBytes)]
        [Arguments(PerformanceInputKind.VbaLikeSource)]
        [Arguments(PerformanceInputKind.MixedPattern)]
        [Arguments(PerformanceInputKind.LowCompressibility)]
        public async Task CompressionPerformanceCanBeMeasured(PerformanceInputKind inputKind)
        {
            var input = PerformanceInputs.Create(inputKind);

            var elapsed = MeasureBestOf(3, () => VbaCompression.Compress(input), out var compressed);

            Console.WriteLine($"{inputKind} compression: {elapsed.TotalMilliseconds:N2} ms, {input.Length:N0} bytes -> {compressed.Length:N0} bytes.");
            await Assert.That(compressed.Length).IsGreaterThan(0);
            await Assert.That(elapsed).IsLessThan(TimeSpan.FromSeconds(30));
        }

        [Test]
        [Arguments(PerformanceInputKind.RepeatedBytes)]
        [Arguments(PerformanceInputKind.VbaLikeSource)]
        [Arguments(PerformanceInputKind.MixedPattern)]
        [Arguments(PerformanceInputKind.LowCompressibility)]
        public async Task DecompressionPerformanceCanBeMeasured(PerformanceInputKind inputKind)
        {
            var input = PerformanceInputs.Create(inputKind);
            var compressed = VbaCompression.Compress(input);

            var elapsed = MeasureBestOf(5, () => VbaCompression.Decompress(compressed), out var decompressed);

            Console.WriteLine($"{inputKind} decompression: {elapsed.TotalMilliseconds:N2} ms, {compressed.Length:N0} bytes -> {decompressed.Length:N0} bytes.");
            await Assert.That(decompressed.SequenceEqual(input)).IsTrue();
            await Assert.That(elapsed).IsLessThan(TimeSpan.FromSeconds(10));
        }

        private static TimeSpan MeasureBestOf(int iterations, Func<byte[]> operation, out byte[] result)
        {
            operation();

            var best = TimeSpan.MaxValue;
            result = [];

            for (var i = 0; i < iterations; i++)
            {
                var stopwatch = Stopwatch.StartNew();
                var current = operation();
                stopwatch.Stop();

                if (stopwatch.Elapsed < best)
                {
                    best = stopwatch.Elapsed;
                    result = current;
                }
            }

            return best;
        }
    }

    public enum PerformanceInputKind
    {
        RepeatedBytes,
        VbaLikeSource,
        MixedPattern,
        LowCompressibility
    }

    internal static class PerformanceInputs
    {
        private const int InputSize = 64 * 1024;

        public static byte[] Create(PerformanceInputKind inputKind)
        {
            return inputKind switch
            {
                PerformanceInputKind.RepeatedBytes => CreateRepeatedBytes(InputSize),
                PerformanceInputKind.VbaLikeSource => CreateVbaLikeSource(InputSize),
                PerformanceInputKind.MixedPattern => CreateMixedPattern(InputSize),
                PerformanceInputKind.LowCompressibility => CreateLowCompressibility(InputSize),
                _ => throw new ArgumentOutOfRangeException(nameof(inputKind), inputKind, "Unknown performance input.")
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
}
