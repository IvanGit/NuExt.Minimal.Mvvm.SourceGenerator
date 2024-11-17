using Microsoft.CodeAnalysis;
using System.CodeDom.Compiler;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Text;

/* Useful links
 * https://github.com/dotnet/roslyn/blob/main/docs/features/incremental-generators.md
 * https://github.com/dotnet/roslyn/blob/main/docs/features/incremental-generators.cookbook.md
 * https://github.com/dotnet/roslyn-sdk/blob/main/samples/CSharp/SourceGenerators/SourceGeneratorSamples/CSharpSourceGeneratorSamples.csproj
 * https://andrewlock.net/series/creating-a-source-generator/
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
            Localize
        }

        #endregion

        #region Sources

        private static readonly (string hintName, string source)[] s_sources = {
            (hintName : "Minimal.Mvvm.Attributes.g.cs", source : """
                using System;

                /// <summary>
                /// Enum to define access modifiers.
                /// </summary>
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
                    /// <summary>
                    /// A custom attribute that allows specifying a fully qualified attribute name to be applied to a generated property.
                    /// </summary>
                    [AttributeUsage(AttributeTargets.Field, Inherited = false, AllowMultiple = true)]
                    internal sealed class CustomAttributeAttribute : Attribute
                    {
                        /// <summary>
                        /// Initializes a new instance of the <see cref="CustomAttributeAttribute"/> class with the specified fully qualified attribute name.
                        /// </summary>
                        /// <param name="fullyQualifiedAttributeName">The fully qualified name of the attribute to apply.</param>
                        public CustomAttributeAttribute(string fullyQualifiedAttributeName)
                        {
                            FullyQualifiedAttributeName = fullyQualifiedAttributeName;
                        }

                        /// <summary>
                        /// Gets the fully qualified name of the attribute to apply.
                        /// </summary>
                        public string FullyQualifiedAttributeName { get; }
                    }

                    /// <summary>
                    /// Attribute to mark a field for code generation of property and associated callback methods.
                    /// </summary>
                    [AttributeUsage(AttributeTargets.Field, Inherited = false, AllowMultiple = false)]
                    internal sealed class NotifyAttribute : Attribute
                    {
                        /// <summary>
                        /// Initializes a new instance of the <see cref="NotifyAttribute"/> class.
                        /// </summary>
                        public NotifyAttribute()
                        {
                        }

                        /// <summary>
                        /// Initializes a new instance of the <see cref="NotifyAttribute"/> class with the specified property name.
                        /// </summary>
                        /// <param name="propertyName">The name of the property.</param>
                        public NotifyAttribute(string propertyName)
                        {
                            PropertyName = propertyName;
                        }

                        /// <summary>
                        /// Gets or sets the name of the property.
                        /// </summary>
                        public string PropertyName { get; set; }

                        /// <summary>
                        /// Gets or sets the name of the callback method.
                        /// </summary>
                        public string CallbackName { get; set; }

                        /// <summary>
                        /// Gets or sets a value indicating whether to prefer method with parameter for callback.
                        /// </summary>
                        public bool PreferCallbackWithParameter { get; set; }

                        /// <summary>
                        /// Gets or sets the access modifier for the getter.
                        /// </summary>
                        public AccessModifier Getter { get; set; }

                        /// <summary>
                        /// Gets or sets the access modifier for the setter.
                        /// </summary>
                        public AccessModifier Setter { get; set; }
                    }

                    /// <summary>
                    /// Specifies that the target class should be localized using the provided JSON file.
                    /// </summary>
                    [AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
                    internal sealed class LocalizeAttribute : Attribute
                    {
                        public LocalizeAttribute(string jsonFileName)
                        {

                        }
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
                transform: static (context, cancellationToken) => (member: context.TargetSymbol, attributes: context.Attributes, AttributeType.Notify)),
            (fullyQualifiedMetadataName: LocalizePropertyGenerator.LocalizeAttributeFullyQualifiedMetadataName,
                predicate: LocalizePropertyGenerator.Predicate,
                transform: static (context, cancellationToken) => (member: context.TargetSymbol, attributes: context.Attributes, AttributeType.Localize))
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

            var pipeline = context.AdditionalTextsProvider
                .Where(static (text) => text.Path.EndsWith(".json"))
                .Select(static (text, cancellationToken) =>
                {
                    var name = Path.GetFileName(text.Path);
                    return (name, text);
                });

            var combined = context.CompilationProvider.Combine(pipeline.Collect()).Combine(pipelines.MergeSources());

            context.RegisterSourceOutput(combined, static (context, pair) =>
            {
                var ((compilation, additionalTexts), items) = pair;

                var nullableContextOptions = compilation.Options.NullableContextOptions;
                var typeInfos = new Dictionary<INamedTypeSymbol, List<(ISymbol member, ImmutableArray<AttributeData> attributes, AttributeType attributeType)>>(SymbolEqualityComparer.Default);
                foreach (var item in items)
                {
                    var (symbol, attributes, attributeType) = item;
                    switch (attributeType)
                    {
                        case AttributeType.Notify:
                            if (symbol is not IFieldSymbol fieldSymbol || !NotifyPropertyGenerator.Predicate(compilation, fieldSymbol))
                            {
                                continue;
                            }
                            if (!typeInfos.TryGetValue(symbol.ContainingType, out var typeInfo))
                            {
                                typeInfos[symbol.ContainingType] = typeInfo = new();
                            }
                            typeInfo.Add(item);
                            break;
                        case AttributeType.Localize:
                            if (symbol is not INamedTypeSymbol typeSymbol || !LocalizePropertyGenerator.Predicate(compilation, typeSymbol, attributes, additionalTexts))
                            {
                                continue;
                            }
                            if (!typeInfos.TryGetValue(typeSymbol, out typeInfo))
                            {
                                typeInfos[typeSymbol] = typeInfo = new();
                            }
                            typeInfo.Add(item);
                            break;
                    }
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
                                NotifyPropertyGenerator.Generate(writer, members.Select(m => m.member), nullableContextOptions);
                                break;

                            case AttributeType.Localize:
                                LocalizePropertyGenerator.Generate(writer, members.Select(m => (m.member, m.attributes)), additionalTexts, nullableContextOptions);
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
