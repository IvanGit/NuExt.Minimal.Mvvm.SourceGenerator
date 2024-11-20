using Basic.Reference.Assemblies;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Minimal.Mvvm;
using Minimal.Mvvm.SourceGenerator;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Reflection;

namespace NuExt.Minimal.Mvvm.SourceGenerator.Tests
{
    internal abstract class SourceGeneratorTestBase
    {
        protected static readonly string GeneratorName = typeof(Generator).Namespace!;
        protected static readonly Version GeneratorVersion = typeof(Generator).Assembly.GetName().Version!;

        protected static CSharpCompilation Compile(string code, NullableContextOptions nullableContextOptions = NullableContextOptions.Enable)
        {
            var tree = CSharpSyntaxTree.ParseText(code);

            var references =
#if NET8_0_OR_GREATER
                ReferenceAssemblies.Net80.Cast<MetadataReference>()
#elif NETFRAMEWORK
                ReferenceAssemblies.Net472.Cast<MetadataReference>().Concat(new[] { MetadataReference.CreateFromFile(typeof(System.Text.Json.JsonSerializer).GetTypeInfo().Assembly.Location) })
#endif
                    .Concat(new[]
                        { MetadataReference.CreateFromFile(typeof(BindableBase).GetTypeInfo().Assembly.Location) });

            var compilation = CSharpCompilation.Create("HelloWorld.dll",
                new[] { tree },
                references: references,
                new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary,
                    nullableContextOptions: nullableContextOptions));
            return compilation;
        }

        protected static string? GetExpectedSource(string? sourceTemplate)
        {
            return sourceTemplate?.Replace("[GeneratorVersion]", GeneratorVersion.ToString()).Replace("[GeneratorName]", GeneratorName);
        }

        protected static List<string> GetSourceLines(string generatedSource)
        {
            var result = new List<string>();
            foreach (var line in generatedSource.Split(new[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries))
            {
                if (string.IsNullOrWhiteSpace(line))
                {
                    continue;
                }
                result.Add(line.TrimEnd());
            }
            return result;
        }

        protected static void MultipleAssert(CSharpCompilation? outputCompilation, ImmutableArray<Diagnostic> diagnostics,
            GeneratorRunResult generatorResult, string? expectedSource)
        {
            var output = outputCompilation!.SyntaxTrees.ToDictionary(tree => tree.FilePath, tree => tree.ToString());

            Assert.That(outputCompilation, Is.Not.Null);

            if (expectedSource == null)
            {
                Assert.That(outputCompilation!.SyntaxTrees.Length, Is.EqualTo(5));
                Assert.That(diagnostics.IsEmpty, Is.True);// there were no diagnostics created by the generators
                Assert.That(generatorResult.Diagnostics.IsEmpty, Is.True);
                Assert.That(generatorResult.GeneratedSources.Length, Is.EqualTo(4));
                Assert.That(generatorResult.Exception is null);
                return;
            }

            Assert.That(outputCompilation!.SyntaxTrees.Length, Is.EqualTo(6));
            Assert.That(diagnostics.IsEmpty, Is.True);// there were no diagnostics created by the generators
            var allDiagnostics = outputCompilation.GetDiagnostics();
            Assert.That(allDiagnostics.IsEmpty, Is.True); // verify the compilation with the added source has no diagnostics

            Assert.That(generatorResult.Diagnostics.IsEmpty);
            Assert.That(generatorResult.GeneratedSources.Length, Is.EqualTo(5));
            Assert.That(generatorResult.Exception, Is.Null);

            var generatedSource = generatorResult.GeneratedSources.Last().SyntaxTree.ToString();
            var generatedSourceText = generatorResult.GeneratedSources.Last().SourceText.ToString();
            Assert.That(generatedSource, Is.EqualTo(generatedSourceText));

            var generatedSourceLines = GetSourceLines(generatedSource);
            var expectedSourceLines = GetSourceLines(expectedSource!);

            Assert.That(generatedSourceLines, Is.EqualTo(expectedSourceLines));
        }

        protected static (CSharpCompilation? outputCompilation, ImmutableArray<Diagnostic> diagnostics, GeneratorRunResult generatorResult) RunGenerator(CSharpCompilation compilation)
        {
            return RunGenerator(compilation, ImmutableArray<AdditionalText>.Empty);
        }

        protected static (CSharpCompilation? outputCompilation, ImmutableArray<Diagnostic> diagnostics, GeneratorRunResult generatorResult) RunGenerator(CSharpCompilation compilation, ImmutableArray<AdditionalText> additionalTexts)
        {
            IIncrementalGenerator generator = new Generator();
            var sourceGenerator = generator.AsSourceGenerator();

            // Create the driver that will control the generation, passing in our generator
            GeneratorDriver driver = CSharpGeneratorDriver.Create(sourceGenerator);

            if (additionalTexts != null && additionalTexts.Length > 0)
            {
                driver = driver.AddAdditionalTexts(additionalTexts);
            }

            // Run the generation pass
            // (Note: the generator driver itself is immutable, and all calls return an updated version of the driver that you should use for subsequent calls)
            driver = driver.RunGeneratorsAndUpdateCompilation(compilation, out var outputCompilation, out var diagnostics);

            GeneratorDriverRunResult runResult = driver.GetRunResult();
            Debug.Assert(runResult.Results.Length == 1);
            GeneratorRunResult generatorResult = runResult.Results[0];

            return (outputCompilation as CSharpCompilation, diagnostics, generatorResult);
        }
    }
}
