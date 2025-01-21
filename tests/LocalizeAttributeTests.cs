using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using System.Collections.Immutable;

namespace NuExt.Minimal.Mvvm.SourceGenerator.Tests
{
    internal class LocalizeAttributeTests : SourceGeneratorTestBase
    {
        [Test]
        public void NotifyAttributePropertyNamesTest()
        {
            var sources = LocalizeAttributes.Sources;

            var basePath = AppDomain.CurrentDomain.BaseDirectory;
            var jsonFilePath = Path.Combine(basePath, "Config/local.en-US.json");

            Assert.That(File.Exists(jsonFilePath), Is.True);

            foreach (var (source, expected) in sources)
            {
                var compilation = Compile(source);
                var (outputCompilation, diagnostics, generatorResult) = RunGenerator(compilation, ImmutableArray.Create<AdditionalText>(new AdditionalTextFileWrapper(jsonFilePath)));
                MultipleAssert(outputCompilation, diagnostics, generatorResult, GetExpectedSource(expected), false);
            }
        }

        public class AdditionalTextFileWrapper : AdditionalText
        {
            private readonly string _path;

            public AdditionalTextFileWrapper(string path)
            {
                _path = path;
            }

            public override string Path => _path;

            public override SourceText GetText(CancellationToken cancellationToken = default)
            {
                return SourceText.From(File.ReadAllText(Path));
            }
        }
    }
}
