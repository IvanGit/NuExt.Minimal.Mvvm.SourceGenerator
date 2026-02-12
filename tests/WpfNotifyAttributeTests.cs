using Minimal.Mvvm.SourceGenerator;

namespace NuExt.Minimal.Mvvm.SourceGenerator.Tests
{
    public class WpfNotifyAttributeTests : CSharpSourceGeneratorTestBase
    {
        [Test]
        public async Task UseCommandManagerCommandsTest()
        {
            var sources = Commands.UseCommandManagerSources;

            AddReferencedAssemblies();

            foreach (var (fileName, hintName, source, expected, additionalFiles) in sources)
            {
                TestState.Sources.Add((fileName, source));

                AddGeneratedSources();
                AddExpectedSource(hintName, expected);
                AddAdditionalGeneratedSources(additionalFiles);
                AddAdditionalGeneratedSources([Generator.RequerySuggestedEventManagerSource]);
                break;
            }

            await RunAsync(CancellationToken.None);
        }
    }
}
