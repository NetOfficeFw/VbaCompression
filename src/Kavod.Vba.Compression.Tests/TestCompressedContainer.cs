using System.IO;
using System.Linq;

namespace Kavod.Vba.Compression.Tests
{
    public class TestCompressedContainer
    {
        private readonly byte[] _validCompressedDirStream;
        private readonly byte[] _validDecompressedDirStream;

        public TestCompressedContainer()
        {
            _validCompressedDirStream = File.ReadAllBytes(@"Test Files/ValidCompressedDirStream");
            _validDecompressedDirStream = File.ReadAllBytes(@"Test Files/ValidDecompressedDirStream");
        }

        [Test]
        public async Task CanCreateCompressedContainer()
        {
            var container = new CompressedContainer(_validCompressedDirStream);

            await Assert.That(container).IsTypeOf<CompressedContainer>();
        }


        [Test]
        public async Task DecompressedDataSameAsMicrosoftImplementation()
        {
            var container = new CompressedContainer(_validCompressedDirStream);
            var buffer = new DecompressedBuffer(container);

            await Assert.That(buffer.Data.SequenceEqual(_validDecompressedDirStream)).IsTrue();
        }


        [Test]
        public async Task ParsedCompressedDataIsSameAsInput()
        {
            var container = new CompressedContainer(_validCompressedDirStream);

            await Assert.That(container.SerializeData().SequenceEqual(_validCompressedDirStream)).IsTrue();
        }

        [Test]
        [Skip("Does not pass.")]
        public async Task CompressedDataSameAsMicrosoftImplementation()
        {
            var buffer = new DecompressedBuffer(_validDecompressedDirStream);
            var container = new CompressedContainer(buffer);

            await Assert.That(container.SerializeData().SequenceEqual(_validCompressedDirStream)).IsTrue();
        }

        [Test]
        public async Task CompressDecompressDataAreEqual()
        {
            var buffer = new DecompressedBuffer(_validDecompressedDirStream);
            var container = new CompressedContainer(buffer);
            var newBuffer = new DecompressedBuffer(container);

            await Assert.That(newBuffer.Data.SequenceEqual(_validDecompressedDirStream)).IsTrue();
        }

        [Test]
        public async Task GivenCompressedDataThatSerializingItReproducesSameData()
        {
            var refCompressed = new CompressedContainer(_validCompressedDirStream);

            var actual = refCompressed.SerializeData();

            await Assert.That(actual.Length).IsEqualTo(_validCompressedDirStream.Length);
            await Assert.That(actual.SequenceEqual(_validCompressedDirStream)).IsTrue();
        }

        [Test]
        [Skip("Does not pass.")]
        public async Task TestDirStreamCompression()
        {
            await CompressionTestHelper.LowLevelCompressionComparison(_validDecompressedDirStream, _validCompressedDirStream);
        }
    }
}
