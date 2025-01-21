namespace NuExt.Minimal.Mvvm.SourceGenerator.Tests
{
    internal class NotifyAttributeTests : SourceGeneratorTestBase
    {
        [Test]
        public void NotifyAttributeAlsoNotifyAttributesTest()
        {
            var sources = AlsoNotifyAttributes.EventArgsCacheSources;

            foreach (var (source, expected) in sources)
            {
                var compilation = Compile(source);
                var (outputCompilation, diagnostics, generatorResult) = RunGenerator(compilation);
                MultipleAssert(outputCompilation, diagnostics, generatorResult, GetExpectedSource(expected));
            }
        }

        [Test]
        public void NotifyAttributePropertyNamesTest()
        {
            var sources = PropertyNames.EventArgsCacheSources;

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
            var sources = AccessModifiers.EventArgsCacheSources;

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
            var sources = Callbacks.EventArgsCacheSources;

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
            var sources = CustomAttributes.EventArgsCacheSources;

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
            var sources = Commands.EventArgsCacheSources;

            foreach (var (source, expected) in sources)
            {
                var compilation = Compile(source);
                var (outputCompilation, diagnostics, generatorResult) = RunGenerator(compilation);
                MultipleAssert(outputCompilation, diagnostics, generatorResult, GetExpectedSource(expected));
            }
        }
    }
}
