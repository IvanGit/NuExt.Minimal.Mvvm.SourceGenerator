using System.CodeDom.Compiler;

namespace Minimal.Mvvm.SourceGenerator
{
    internal struct EventArgsCacheGenerator
    {
        internal const string EventArgsCacheFullyQualifiedName = "global::Minimal.Mvvm.EventArgsCache";

        #region Methods

        public static void Generate(IndentedTextWriter writer, HashSet<string> propertyNames)
        {
            writer.WriteLine("internal static partial class EventArgsCache");
            writer.WriteLine("{");
            writer.Indent++;

            foreach (var propertyName in propertyNames)
            {
                writer.WriteLine($"""internal static readonly global::System.ComponentModel.PropertyChangedEventArgs {propertyName}PropertyChanged = new global::System.ComponentModel.PropertyChangedEventArgs("{propertyName}");""");
            }

            writer.Indent--;
            writer.WriteLine("}");
        }

        #endregion
    }
}
