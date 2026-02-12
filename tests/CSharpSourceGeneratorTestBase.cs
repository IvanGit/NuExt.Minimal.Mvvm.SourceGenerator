using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Testing;
using Microsoft.CodeAnalysis.Testing;
using Minimal.Mvvm;
using Minimal.Mvvm.SourceGenerator;

namespace NuExt.Minimal.Mvvm.SourceGenerator.Tests
{
    public class CSharpSourceGeneratorTestBase : CSharpSourceGeneratorTest<Generator, DefaultVerifier>
    {
        protected static readonly string GeneratorName = typeof(Generator).Namespace!;
        protected static readonly Version GeneratorVersion = typeof(Generator).Assembly.GetName().Version!;

        protected override CompilationOptions CreateCompilationOptions()
        {
            var options = new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary, allowUnsafe: true);
            options = options.WithNullableContextOptions(NullableContextOptions.Enable);
            return options;
        }

        public void AddGeneratedSources()
        {
            foreach (var (hintName, source) in Generator.Sources)
            {
                TestState.GeneratedSources.Add((typeof(Generator), hintName, source));
            }
        }

        public void AddExpectedSource(string hintName, string expected)
        {
            TestState.GeneratedSources.Add((typeof(Generator), hintName, GetExpectedSource(expected)));
        }

        public void AddAdditionalGeneratedSources((string hintName, string expected)[] additionalFiles)
        {
            if (additionalFiles != null)
            {
                foreach (var (hintName, source) in additionalFiles)
                {
                    TestState.GeneratedSources.Add((typeof(Generator), hintName, GetExpectedSource(source)));
                }
            }
        }

        public void AddReferencedAssemblies()
        {
#if NET10_0
            ReferenceAssemblies = ReferenceAssemblies.Net.Net100Windows;
#elif NET9_0
            ReferenceAssemblies = ReferenceAssemblies.Net.Net90Windows;
#elif NET8_0
            ReferenceAssemblies = ReferenceAssemblies.Net.Net80Windows;
#endif
            TestState.AdditionalReferences.Add(
                MetadataReference.CreateFromFile(typeof(BindableBase).Assembly.Location)
            );
        }

        protected static string GetExpectedSource(string? sourceTemplate)
        {
            return sourceTemplate != null ? sourceTemplate.Replace("[GeneratorVersion]", GeneratorVersion.ToString()).Replace("[GeneratorName]", GeneratorName) : "";
        }
    }
}
