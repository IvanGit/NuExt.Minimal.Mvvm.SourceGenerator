using Basic.Reference.Assemblies;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Minimal.Mvvm;
using Minimal.Mvvm.SourceGenerator;
using System.Reflection;
using System.Xml.Linq;

/*
 * https://github.com/dotnet/roslyn/blob/main/docs/features/source-generators.cookbook.md#unit-testing-of-generators
 * https://www.meziantou.net/testing-roslyn-incremental-source-generators.htm
 */

namespace NuExt.Minimal.Mvvm.SourceGenerator.Tests
{
    public class Tests
    {
        [SetUp]
        public void Setup()
        {
        }

        [Test]
        public void Test1()
        {

            const string code = """
                using Minimal.Mvvm;

                public partial class MyModel : ViewModelBase
                {
                    [Notify]
                    private string _name = null!;

                    [Notify]
                    private string? _description;
                }
                """;
            var tree = CSharpSyntaxTree.ParseText(code);

            var references =
#if NET8_0
                ReferenceAssemblies.Net80
#else
                ReferenceAssemblies.Net472
#endif
                    .Cast<MetadataReference>().Concat(new[]
                        { MetadataReference.CreateFromFile(typeof(BindableBase).GetTypeInfo().Assembly.Location) });

            var compilation = CSharpCompilation.Create("HelloWorld.dll",
                new[] { tree }, 
                references: references,
                new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary,
                    nullableContextOptions: NullableContextOptions.Enable));


            IIncrementalGenerator generator = new Generator();
            var sourceGenerator = generator.AsSourceGenerator();

            // Create the driver that will control the generation, passing in our generator
            GeneratorDriver driver = CSharpGeneratorDriver.Create(sourceGenerator);

            // Run the generation pass
            // (Note: the generator driver itself is immutable, and all calls return an updated version of the driver that you should use for subsequent calls)
            driver = driver.RunGeneratorsAndUpdateCompilation(compilation, out var outputCompilation, out var diagnostics);

            // We can now assert things about the resulting compilation:
            Assert.That(diagnostics.IsEmpty); // there were no diagnostics created by the generators
            Assert.That(outputCompilation.SyntaxTrees.Count(), Is.EqualTo(3));
            var allDiagnostics = outputCompilation.GetDiagnostics();
            Assert.That(allDiagnostics.IsEmpty); // verify the compilation with the added source has no diagnostics

            // Or we can look at the results directly:
            GeneratorDriverRunResult runResult = driver.GetRunResult();

            // The runResult contains the combined results of all generators passed to the driver
            Assert.That(runResult.GeneratedTrees.Length, Is.EqualTo(2));
            Assert.That(runResult.Diagnostics.IsEmpty);

            // Or you can access the individual results on a by-generator basis
            GeneratorRunResult generatorResult = runResult.Results[0];
            //Assert.That(generatorResult.Generator, Is.EqualTo(generator));
            Assert.That(generatorResult.Diagnostics.IsEmpty);
            Assert.That(generatorResult.GeneratedSources.Length, Is.EqualTo(2));
            Assert.That(generatorResult.Exception, Is.Null);

            Assert.Pass();
        }

        private partial class MyModel : ViewModelBase
        {
            [Notify("MyName", CallbackName = nameof(OnCurrentViewModelChanged), PreferCallbackWithParameter = true)]
            private string _name = null!;
            //get is internal, set is protected
            protected internal string Name
            {
                get => _name;
                protected set => SetProperty(ref _name, value);
            }

            private string? _description;
            //get is protected, set is internal
            protected internal string? Description
            {
                get => _description;
                internal set => SetProperty(ref _description, value);
            }
        }

        partial class MyModel
        {
            private global::Minimal.Mvvm.ViewModelBase? _currentViewModel;

            [global::System.Text.Json.Serialization.JsonIgnore]
            [global::System.Text.Json.Serialization.JsonPropertyName("Name")]
            public global::Minimal.Mvvm.ViewModelBase? CurrentViewModel
            {
                get => _currentViewModel;
                set => SetProperty(ref _currentViewModel, value, _onCurrentViewModelChanged ??= OnCurrentViewModelChanged);
            }

            private Action<ViewModelBase?>? _onCurrentViewModelChanged;

            private void OnCurrentViewModelChanged()
            {
            }

            private void OnCurrentViewModelChanged(BindableBase? oldCurrentViewModel)
            {
            }

            private void OnCurrentViewModelChanged(ViewModelBase? oldCurrentViewModel)
            {
            }

            private void OnCurrentViewModelChanged(MyModel? oldCurrentViewModel)
            {
            }
        }
    }
}