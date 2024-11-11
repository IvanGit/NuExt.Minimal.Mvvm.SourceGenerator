using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using System.Collections.Immutable;

namespace NuExt.Minimal.Mvvm.SourceGenerator.Tests
{
    internal class NotifyAttributeTests: SourceGeneratorTestBase
    {
        [Test]
        public void NotifyAttributeTest()
        {
            var sources = BindableBaseSource.Sources;

            foreach (var (source, expected) in sources)
            {
                var compilation = Compile(source);
                var (outputCompilation, diagnostics, generatorResult) = RunGenerator(compilation);
                MultipleAssert(outputCompilation, diagnostics, generatorResult, GetExpectedSource(expected));
            }
        }

        private static void MultipleAssert(CSharpCompilation? outputCompilation, ImmutableArray<Diagnostic> diagnostics,
            GeneratorRunResult generatorResult, string? expectedSource)
        {
            var output = outputCompilation!.SyntaxTrees.ToDictionary(tree => tree.FilePath, tree => tree.ToString());

            Assert.That(outputCompilation, Is.Not.Null);

            if (expectedSource == null)
            {
                Assert.That(outputCompilation!.SyntaxTrees.Length == 2);
                Assert.That(diagnostics.IsEmpty);// there were no diagnostics created by the generators
                Assert.That(generatorResult.Diagnostics.IsEmpty);
                Assert.That(generatorResult.GeneratedSources.Length == 1);
                Assert.That(generatorResult.Exception is null);
                return;
            }

            Assert.That(outputCompilation!.SyntaxTrees.Length == 3);
            Assert.That(diagnostics.IsEmpty);// there were no diagnostics created by the generators
            var allDiagnostics = outputCompilation.GetDiagnostics();
            Assert.That(allDiagnostics.IsEmpty); // verify the compilation with the added source has no diagnostics

            Assert.That(generatorResult.Diagnostics.IsEmpty);
            Assert.That(generatorResult.GeneratedSources.Length == 2);
            Assert.That(generatorResult.Exception is null);

            var generatedSource = generatorResult.GeneratedSources.Last().SyntaxTree.ToString();
            var generatedSourceText = generatorResult.GeneratedSources.Last().SourceText.ToString();
            Assert.That(generatedSource, Is.EqualTo(generatedSourceText));

            var generatedSourceLines = GetSourceLines(generatedSource);
            var expectedSourceLines = GetSourceLines(expectedSource!);

            Assert.That(generatedSourceLines, Is.EqualTo(expectedSourceLines));
        }
    }
}
