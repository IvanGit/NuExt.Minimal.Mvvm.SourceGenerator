using Basic.Reference.Assemblies;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Minimal.Mvvm;
using Minimal.Mvvm.SourceGenerator;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
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

        protected static (CSharpCompilation? outputCompilation, ImmutableArray<Diagnostic> diagnostics, GeneratorRunResult generatorResult) RunGenerator(CSharpCompilation compilation)
        {
            IIncrementalGenerator generator = new Generator();
            var sourceGenerator = generator.AsSourceGenerator();

            // Create the driver that will control the generation, passing in our generator
            GeneratorDriver driver = CSharpGeneratorDriver.Create(sourceGenerator);

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
