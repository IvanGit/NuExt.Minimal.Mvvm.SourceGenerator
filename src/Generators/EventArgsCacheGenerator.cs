using System.CodeDom.Compiler;

namespace Minimal.Mvvm.SourceGenerator
{
    internal readonly ref struct EventArgsCacheGeneratorContext(IndentedTextWriter writer, List<string> propertyNames)
    {
        internal readonly List<string> PropertyNames = propertyNames;
        internal readonly IndentedTextWriter Writer = writer;
    }

    internal struct EventArgsCacheGenerator
    {
        internal const string GeneratedClassFullyQualifiedName = $"global::{GeneratedNamespace}.{GeneratedClassName}";
        internal const string GeneratedClassName = "EventArgsCache";
        internal const string GeneratedNamespace = "Minimal.Mvvm";

        #region Methods

        public static void Generate(scoped EventArgsCacheGeneratorContext ctx)
        {
            ctx.Writer.WriteLine($"internal static partial class {GeneratedClassName}");
            ctx.Writer.WriteLine("{");
            ctx.Writer.Indent++;

            foreach (var propertyName in ctx.PropertyNames)
            {
                ctx.Writer.WriteLine($"""internal static readonly global::System.ComponentModel.PropertyChangedEventArgs {propertyName}PropertyChanged = new global::System.ComponentModel.PropertyChangedEventArgs("{propertyName}");""");
            }

            ctx.Writer.Indent--;
            ctx.Writer.WriteLine("}");
        }

        #endregion
    }
}
