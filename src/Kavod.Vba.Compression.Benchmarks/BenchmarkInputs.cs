using System.Reflection;
using System.Text;

namespace Kavod.Vba.Compression.Benchmarks;

internal static class BenchmarkInputs
{
    private const int InputSize = 256 * 1024;
    private static readonly Lazy<byte[]> ExcelVbaCorpus = new(CreateExcelVbaCorpus);
    private static readonly Lazy<byte[]> ExcelVbaLargestModule = new(CreateExcelVbaLargestModule);

    public static IReadOnlyList<string> InputNames { get; } =
    [
        "ExcelVbaCorpus",
        "ExcelVbaCorpusRepeated",
        "ExcelVbaLargestModuleRepeated",
        "ExcelVbaCorpusWithNoise",
        "LowCompressibility"
    ];

    public static byte[] Create(string inputName)
    {
        return inputName switch
        {
            "ExcelVbaCorpus" => ExcelVbaCorpus.Value.ToArray(),
            "ExcelVbaCorpusRepeated" => RepeatBytes(ExcelVbaCorpus.Value, InputSize),
            "ExcelVbaLargestModuleRepeated" => RepeatBytes(ExcelVbaLargestModule.Value, InputSize),
            "ExcelVbaCorpusWithNoise" => CreateExcelVbaCorpusWithNoise(InputSize),
            "LowCompressibility" => CreateLowCompressibility(InputSize),
            _ => throw new ArgumentOutOfRangeException(nameof(inputName), inputName, "Unknown benchmark input.")
        };
    }

    private static byte[] CreateExcelVbaCorpus()
    {
        var assembly = Assembly.GetExecutingAssembly();
        var resourceNames = GetExcelVbaSourceResourceNames(assembly);
        var builder = new StringBuilder();

        foreach (var resourceName in resourceNames)
        {
            builder.AppendLine("' ---- " + GetSourceFileName(resourceName) + " ----");
            builder.AppendLine(ReadResourceText(assembly, resourceName));
        }

        return Encoding.UTF8.GetBytes(builder.ToString());
    }

    private static byte[] CreateExcelVbaLargestModule()
    {
        var assembly = Assembly.GetExecutingAssembly();
        return GetExcelVbaSourceResourceNames(assembly)
            .Select(resourceName => Encoding.UTF8.GetBytes(ReadResourceText(assembly, resourceName)))
            .OrderByDescending(bytes => bytes.Length)
            .First();
    }

    private static byte[] CreateExcelVbaCorpusWithNoise(int size)
    {
        var buffer = new byte[size];
        var source = ExcelVbaCorpus.Value;
        var random = CreateLowCompressibility(size / 4);

        for (var i = 0; i < buffer.Length; i++)
        {
            buffer[i] = i % 4 == 0
                ? random[i / 4]
                : source[i % source.Length];
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

    private static byte[] RepeatBytes(byte[] source, int size)
    {
        var buffer = new byte[size];

        for (var offset = 0; offset < buffer.Length; offset += source.Length)
        {
            var count = Math.Min(source.Length, buffer.Length - offset);
            Array.Copy(source, 0, buffer, offset, count);
        }

        return buffer;
    }

    private static string[] GetExcelVbaSourceResourceNames(Assembly assembly)
    {
        var resourceNames = assembly.GetManifestResourceNames()
            .Where(resourceName => resourceName.Contains(".InputFiles.ExcelVBA."))
            .Where(resourceName => resourceName.EndsWith(".vb", StringComparison.Ordinal)
                                   || resourceName.EndsWith(".vba", StringComparison.Ordinal))
            .Order(StringComparer.Ordinal)
            .ToArray();

        if (resourceNames.Length == 0)
        {
            throw new InvalidOperationException("The ExcelVBA embedded benchmark inputs were not found.");
        }

        return resourceNames;
    }

    private static string ReadResourceText(Assembly assembly, string resourceName)
    {
        using var stream = assembly.GetManifestResourceStream(resourceName)
                           ?? throw new InvalidOperationException($"Embedded resource '{resourceName}' was not found.");
        using var reader = new StreamReader(stream, Encoding.UTF8, detectEncodingFromByteOrderMarks: true);
        return reader.ReadToEnd();
    }

    private static string GetSourceFileName(string resourceName)
    {
        const string marker = ".InputFiles.ExcelVBA.";
        var markerIndex = resourceName.IndexOf(marker, StringComparison.Ordinal);
        return markerIndex < 0
            ? resourceName
            : resourceName[(markerIndex + marker.Length)..];
    }
}
