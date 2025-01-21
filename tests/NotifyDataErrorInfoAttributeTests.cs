namespace NuExt.Minimal.Mvvm.SourceGenerator.Tests
{
    internal class NotifyDataErrorInfoAttributeTests : SourceGeneratorTestBase
    {
        [Test]
        public void NotifyDataErrorInfoAttributesTest()
        {
            var sources = NotifyDataErrorInfoAttributes.EventArgsCacheSources;

            foreach (var (source, expected) in sources)
            {
                var compilation = Compile(source);
                var (outputCompilation, diagnostics, generatorResult) = RunGenerator(compilation);
                MultipleAssert(outputCompilation, diagnostics, generatorResult, GetExpectedSource(expected));
            }
        }
    }
}
