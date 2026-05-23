using BenchmarkDotNet.Attributes;

namespace Kavod.Vba.Compression.Benchmarks;

[MemoryDiagnoser]
[ShortRunJob]
public class CompressionMemoryBenchmarks
{
    private byte[] _input = [];

    [ParamsSource(nameof(InputNames))]
    public string InputName { get; set; } = "";

    public static IEnumerable<string> InputNames => BenchmarkInputs.InputNames;

    [GlobalSetup]
    public void Setup()
    {
        _input = BenchmarkInputs.Create(InputName);
    }

    [Benchmark]
    public byte[] Compress()
    {
        return VbaCompression.Compress(_input);
    }
}
