using BenchmarkDotNet.Attributes;

namespace Kavod.Vba.Compression.Benchmarks;

[MemoryDiagnoser]
[ShortRunJob]
public class DecompressionMemoryBenchmarks
{
    private byte[] _compressedInput = [];

    [ParamsSource(nameof(InputNames))]
    public string InputName { get; set; } = "";

    public static IEnumerable<string> InputNames => BenchmarkInputs.InputNames;

    [GlobalSetup]
    public void Setup()
    {
        var input = BenchmarkInputs.Create(InputName);
        _compressedInput = VbaCompression.Compress(input);
    }

    [Benchmark]
    public byte[] Decompress()
    {
        return VbaCompression.Decompress(_compressedInput);
    }
}
