using Microsoft.CodeAnalysis;
using System.CodeDom.Compiler;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Text;

/*
 * https://github.com/dotnet/roslyn/blob/main/docs/features/incremental-generators.md
 * https://github.com/dotnet/roslyn/blob/main/docs/features/incremental-generators.cookbook.md
 * https://andrewlock.net/exploring-dotnet-6-part-9-source-generator-updates-incremental-generators/
 */

namespace Minimal.Mvvm.SourceGenerator
{
    [Generator(LanguageNames.CSharp)]
    public class Generator : IIncrementalGenerator
    {
        #region Internal Types

        private enum AttributeType
        {
            Notify,
        }

        #endregion

        #region Sources

        private static readonly (string hintName, string source)[] s_sources = {
            (hintName : "NotifyAttribute.g.cs", source : """
                using System;

                internal enum AccessModifier
                {
                    Default = 0,
                    Public = 6,
                    ProtectedInternal = 5,
                    Internal = 4,
                    Protected = 3,
                    PrivateProtected = 2,
                    Private = 1,
                }

                namespace Minimal.Mvvm
                {
                    [AttributeUsage(AttributeTargets.Field, Inherited = false, AllowMultiple = false)]
                    internal sealed class NotifyAttribute : Attribute
                    {
                        public NotifyAttribute()
                        {
                        }

                        public string PropertyName { get; set; }

                        public AccessModifier Getter { get; set; }

                        public AccessModifier Setter { get; set; }
                    }
                }
                """)
        };

        #endregion

        #region Pipelines

        private static readonly (string fullyQualifiedMetadataName,
            Func<SyntaxNode, CancellationToken, bool> predicate,
            Func<GeneratorAttributeSyntaxContext, CancellationToken, (ISymbol member, ImmutableArray<AttributeData> attributes, AttributeType attributeType)> transform)[] s_pipelines =
        {
            (fullyQualifiedMetadataName: NotifyPropertyGenerator.NotifyAttributeFullyQualifiedMetadataName,
                predicate: NotifyPropertyGenerator.Predicate,
                transform: static (context, cancellationToken) => (member: context.TargetSymbol, attributes: context.Attributes, AttributeType.Notify))
        };

        #endregion

        #region Methods

        public void Initialize(IncrementalGeneratorInitializationContext context)
        {
#if DEBUG
            if (!Debugger.IsAttached)
            {
                //Debugger.Launch();
            }
#endif

            context.RegisterPostInitializationOutput(static postInitializationContext =>
            {
                foreach ((string hintName, string source) in s_sources)
                {
                    postInitializationContext.AddSource(hintName, source);
                }
            });

            var pipelines = s_pipelines.Select(pipeline =>
            {
                var (fullyQualifiedMetadataName, predicate, transform) = pipeline;
                return context.SyntaxProvider.ForAttributeWithMetadataName(
                        fullyQualifiedMetadataName: fullyQualifiedMetadataName, predicate: predicate,
                        transform: transform);
            }).ToList();

            var combined = context.CompilationProvider.Combine(pipelines.MergeSources());

            context.RegisterSourceOutput(combined, static (context, pair) =>
            {
                var (compilation, items) = pair;

                var nullableContextOptions = compilation.Options.NullableContextOptions;
                var typeInfos = new Dictionary<INamedTypeSymbol, List<(ISymbol member, ImmutableArray<AttributeData> attributes, AttributeType attributeType)>>(SymbolEqualityComparer.Default);
                foreach (var (symbol, attributes, attributeType) in items)
                {
                    switch (attributeType)
                    {
                        case AttributeType.Notify:
                            if (symbol is not IFieldSymbol fieldSymbol || !NotifyPropertyGenerator.Predicate(compilation, fieldSymbol))
                            {
                                continue;
                            }
                            break;
                    }
                    if (!typeInfos.TryGetValue(symbol.ContainingType, out var typeInfo))
                    {
                        typeInfos[symbol.ContainingType] = new() { (symbol, attributes, attributeType) };
                        continue;
                    }
                    typeInfo.Add((symbol, attributes, attributeType));
                }
                if (typeInfos.Count == 0)
                {
                    return;
                }

                var sb = new StringBuilder();
                var outerTypes = new List<string>();
                foreach (var typeInfo in typeInfos)
                {
                    var containingType = typeInfo.Key;
                    var members = typeInfo.Value;
                    string? containingNamespace = null;
                    if (containingType.ContainingNamespace is { IsGlobalNamespace: false } @namespace)
                    {
                        containingNamespace = @namespace.ToDisplayString(SymbolDisplayFormats.Namespace);
                    }

                    Debug.Assert(sb.Length == 0);
                    using var writer = new IndentedTextWriter(new StringWriter(sb));
                    writer.WriteLine($"""
                    // <auto-generated>
                    //     Auto-generated by Minimal.Mvvm.SourceGenerator {typeof(Generator).Assembly.GetName().Version}
                    // </auto-generated>
                    """);
                    writer.WriteLine();

                    if (nullableContextOptions != NullableContextOptions.Disable)
                    {
                        writer.WriteNullableContext(nullableContextOptions);
                        writer.WriteLine();
                        writer.WriteLine();
                    }

                    if (!string.IsNullOrEmpty(containingNamespace))//begin namespace
                    {
                        writer.WriteLine($"namespace {containingNamespace}");
                        writer.WriteLine("{");
                        writer.Indent++;
                    }

                    outerTypes.Clear();
                    for (var outerType = containingType; outerType != null; outerType = outerType.ContainingType)
                    {
                        outerTypes.Add(outerType.ToDisplayString(SymbolDisplayFormats.TypeDeclaration));
                    }
                    for (int i = outerTypes.Count - 1; i >= 0; i--)
                    {
                        var outerType = outerTypes[i];
                        writer.WriteLine($"partial {outerType}");
                        writer.WriteLine("{");
                        writer.Indent++;
                    }

                    foreach (var group in members.GroupBy(m => m.attributeType))
                    {
                        switch (group.Key)
                        {
                            case AttributeType.Notify:
                                NotifyPropertyGenerator.Generate(writer, members.Select(m => (m.member, m.attributes)));
                                break;
                        }
                    }

                    for (int i = 0; i < outerTypes.Count; i++)
                    {
                        writer.Indent--;
                        writer.WriteLine("}");
                    }

                    if (!string.IsNullOrEmpty(containingNamespace))//end namespace
                    {
                        writer.Indent--;
                        writer.WriteLine("}");
                    }

                    var sourceText = sb.ToString();
                    sb.Clear();

                    sb.Append(containingType.ToDisplayString(SymbolDisplayFormats.GeneratedFileName));
                    if (containingType.Arity > 0)
                    {
                        sb.Append('`');
                        sb.Append(containingType.Arity);
                    }
                    sb.Append(".g.cs");
                    var generatedFileName = sb.ToString();
                    sb.Clear();

                    context.AddSource(generatedFileName, sourceText);
                }// foreach (var pair in typeInfos)
            });
        }

        #endregion
    }
}
