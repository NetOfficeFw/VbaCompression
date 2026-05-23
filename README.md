# VBA Compression library

> Implementation of the [compression algorithm](https://learn.microsoft.com/en-us/openspecs/office_file_formats/ms-ovba/4742b896-b32b-4eb0-8372-fbf01e3c65fd) used in VBA projects within Microsoft Office applications.

**Kavod.Vba.Compression** library implements [\[MS-OVBA\] Compression and Decompression][1] algorithms used
in the Office VBA File Format structures. Office VBA project contains embedded macros and custom forms
for use in Microsoft Office documents.


## License

Source code is licensed under [MIT License](LICENSE)  
Copyright (c) 2016 Ross Knudsen

## Benchmarks

The repository includes a BenchmarkDotNet project for measuring compression and decompression memory usage across different input shapes:

- `RepeatedBytes`: highly compressible repeated data.
- `VbaLikeSource`: repeated VBA module-like text.
- `MixedPattern`: partially compressible text mixed with deterministic byte noise.
- `LowCompressibility`: deterministic pseudo-random bytes.

Run the memory benchmarks from the repository root:

```bash
dotnet run -c Release --project src/Kavod.Vba.Compression.Benchmarks
```

The test project also includes performance smoke tests that measure compression and decompression speed for the same categories:

```bash
dotnet test
```

[\[MS-OVBA\] Intellectual Property Rights Notice][2]

[1]: https://learn.microsoft.com/en-us/openspecs/office_file_formats/ms-ovba/4742b896-b32b-4eb0-8372-fbf01e3c65fd
[2]: https://learn.microsoft.com/en-us/openspecs/office_file_formats/ms-ovba/575462ba-bf67-4190-9fac-c275523c75fc#intellectual-property-rights-notice-for-open-specifications-documentation
