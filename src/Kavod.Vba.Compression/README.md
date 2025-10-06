Library **VbaCompression** implements [\[MS-OVBA\] Compression and Decompression][1] algorithms used
in the Office VBA File Format structures. Office VBA project contains embedded macros and custom forms
for use in Microsoft Office documents.

### Usage

Sample usage to generate Excel workbook file with macro
from the source code files `Module.vb` and `MyClass.vb`:

```csharp
var content = "abcdefghijklmnopqrstuv.";
var contentBytes = Encoding.ASCII.GetBytes(content);
var compressedBytes = VbaCompression.Compress(contentBytes);
// compressedBytes = 01 19 B0 00 61 62 63 64
//                   65 66 67 68 00 69 6A 6B
//                   6C 6D 6E 6F 70 00 71 72
//                   73 74 75 76 2E
```

### Requirements

Library targets the .NET 10 runtime.


### Legal

_This is a fork of the [Kavod.Vba.Compression][4] library._

[\[MS-OVBA\] Intellectual Property Rights Notice][2]

_Project icon [Gold Bars][3] is licensed from Icons8 service
under Universal Multimedia Licensing Agreement for Icons8._  
_See <https://icons8.com/license> for more information_

[1]: https://learn.microsoft.com/en-us/openspecs/office_file_formats/ms-ovba/4742b896-b32b-4eb0-8372-fbf01e3c65fd
[2]: https://learn.microsoft.com/en-us/openspecs/office_file_formats/ms-ovba/575462ba-bf67-4190-9fac-c275523c75fc#intellectual-property-rights-notice-for-open-specifications-documentation
[3]: https://icons8.com/icon/46072/gold-bars
[4]: https://github.com/rossknudsen/Kavod.Vba.Compression
