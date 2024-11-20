namespace NuExt.Minimal.Mvvm.SourceGenerator.Tests
{
    internal class NotifyAttributeTests : SourceGeneratorTestBase
    {
        [Test]
        public void NotifyAttributePropertyNamesTest()
        {
            var sources = PropertyNames.Sources;

            foreach (var (source, expected) in sources)
            {
                var compilation = Compile(source);
                var (outputCompilation, diagnostics, generatorResult) = RunGenerator(compilation);
                MultipleAssert(outputCompilation, diagnostics, generatorResult, GetExpectedSource(expected));
            }
        }

        [Test]
        public void NotifyAttributeAccessModifiersTest()
        {
            var sources = AccessModifiers.Sources;

            foreach (var (source, expected) in sources)
            {
                var compilation = Compile(source);
                var (outputCompilation, diagnostics, generatorResult) = RunGenerator(compilation);
                MultipleAssert(outputCompilation, diagnostics, generatorResult, GetExpectedSource(expected));
            }
        }

        [Test]
        public void NotifyAttributeCallbacksTest()
        {
            var sources = Callbacks.Sources;

            foreach (var (source, expected) in sources)
            {
                var compilation = Compile(source);
                var (outputCompilation, diagnostics, generatorResult) = RunGenerator(compilation);
                MultipleAssert(outputCompilation, diagnostics, generatorResult, GetExpectedSource(expected));
            }
        }

        [Test]
        public void NotifyAttributeCustomAttributesTest()
        {
            var sources = CustomAttributes.Sources;

            foreach (var (source, expected) in sources)
            {
                var compilation = Compile(source);
                var (outputCompilation, diagnostics, generatorResult) = RunGenerator(compilation);
                MultipleAssert(outputCompilation, diagnostics, generatorResult, GetExpectedSource(expected));
            }
        }

        [Test]
        public void NotifyAttributeCommandsTest()
        {
            var sources = Commands.Sources;

            foreach (var (source, expected) in sources)
            {
                var compilation = Compile(source);
                var (outputCompilation, diagnostics, generatorResult) = RunGenerator(compilation);
                MultipleAssert(outputCompilation, diagnostics, generatorResult, GetExpectedSource(expected));
            }
        }
    }
}
