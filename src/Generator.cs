using Microsoft.CodeAnalysis;
using System.CodeDom.Compiler;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Text;

/* Useful links
 * https://github.com/dotnet/roslyn/blob/main/docs/features/incremental-generators.md
 * https://github.com/dotnet/roslyn/blob/main/docs/features/incremental-generators.cookbook.md
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
            NotifyDataErrorInfo,
            Localize
        }

        #endregion

        #region Sources

        private static readonly (string hintName, string source)[] s_sources = [
            (hintName : "Minimal.Mvvm.AccessModifier.g.cs", source : """
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
            """),
            (hintName : "Minimal.Mvvm.CustomAttributeAttribute.g.cs", source : """
            using System;

            namespace Minimal.Mvvm
            {
                /// <summary>
                /// A custom attribute that allows specifying a fully qualified attribute name to be applied to a generated property.
                /// </summary>
                [AttributeUsage(AttributeTargets.Field | AttributeTargets.Method, Inherited = false, AllowMultiple = true)]
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
            }
            """),
            (hintName : "Minimal.Mvvm.LocalizeAttribute.g.cs", source : """
            using System;
            using System.Linq;

            namespace Minimal.Mvvm
            {
                /// <summary>
                /// Specifies that the target class should be localized using the provided JSON file. JSON file should be specified in AdditionalFiles.
                /// </summary>
                [AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
                internal sealed class LocalizeAttribute : Attribute
                {
                    /// <summary>
                    /// Initializes a new instance of the <see cref="LocalizeAttribute"/> class with the specified JSON file name.
                    /// </summary>
                    /// <param name="jsonFileName">The JSON file name.</param>
                    public LocalizeAttribute(string jsonFileName)
                    {

                    }

                    public static string StringToValidPropertyName(string key)
                    {
                        var s = key.Trim();
                        var validName = char.IsLetter(s[0]) ? char.ToUpper(s[0]).ToString() : "_";
                        validName += new string(s.Skip(1).Select(ch => char.IsLetterOrDigit(ch) ? ch : '_').ToArray());
                        return validName;
                    }
                }
            }
            """),
            (hintName : "Minimal.Mvvm.NotifyAttribute.g.cs", source : """
            using System;

            namespace Minimal.Mvvm
            {
                /// <summary>
                /// Attribute to mark a field or method for code generation of property and associated callback methods.
                /// </summary>
                [AttributeUsage(AttributeTargets.Field | AttributeTargets.Method, Inherited = false, AllowMultiple = false)]
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
            }
            """),
             (hintName : "Minimal.Mvvm.AlsoNotifyAttribute.g.cs", source : """
            using System;

            namespace Minimal.Mvvm
            {
                /// <summary>
                /// Attribute to specify additional properties to notify when the annotated property changes.
                /// </summary>
                [AttributeUsage(AttributeTargets.Field | AttributeTargets.Method, Inherited = false, AllowMultiple = true)]
                internal sealed class AlsoNotifyAttribute : Attribute
                {
                    /// <summary>
                    /// Initializes a new instance of the <see cref="AlsoNotifyAttribute"/> class with the specified property names.
                    /// </summary>
                    /// <param name="propertyNames">The names of the properties to notify.</param>
                    public AlsoNotifyAttribute(params string[] propertyNames)
                    {
                        PropertyNames = propertyNames;
                    }

                    /// <summary>
                    /// Gets the names of the properties to notify.
                    /// </summary>
                    public string[] PropertyNames { get; }
                }
            }
            """),
             (hintName : "Minimal.Mvvm.NotifyDataErrorInfoAttribute.g.cs", source : """
            using System;

            namespace Minimal.Mvvm
            {
                /// <summary>
                /// Attribute to mark a class for code generation if it inherited from <see cref="System.ComponentModel.INotifyDataErrorInfo"/> .
                /// </summary>
                [AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
                internal sealed class NotifyDataErrorInfoAttribute : Attribute
                {
                    /// <summary>
                    /// Initializes a new instance of the <see cref="NotifyDataErrorInfoAttribute"/> class.
                    /// </summary>
                    public NotifyDataErrorInfoAttribute()
                    {

                    }
                }
            }
            """),
        ];

        #endregion

        #region Pipelines

        private static readonly (string fullyQualifiedMetadataName,
            Func<SyntaxNode, CancellationToken, bool> predicate,
            Func<GeneratorAttributeSyntaxContext, CancellationToken, (ISymbol member, ImmutableArray<AttributeData> attributes, AttributeType attributeType)> transform)[] s_pipelines =
        [
            (fullyQualifiedMetadataName: NotifyPropertyGenerator.NotifyAttributeFullyQualifiedName,
                predicate: NotifyPropertyGenerator.IsValidSyntaxNode,
                transform: static (context, _) => (member: context.TargetSymbol, attributes: context.Attributes, AttributeType.Notify)),
            (fullyQualifiedMetadataName: NotifyDataErrorInfoGenerator.NotifyDataErrorInfoAttributeFullyQualifiedName,
                predicate: NotifyDataErrorInfoGenerator.IsValidSyntaxNode,
                transform: static (context, _) => (member: context.TargetSymbol, attributes: context.Attributes, AttributeType.NotifyDataErrorInfo)),
            (fullyQualifiedMetadataName: LocalizePropertyGenerator.LocalizeAttributeFullyQualifiedName,
                predicate: LocalizePropertyGenerator.IsValidSyntaxNode,
                transform: static (context, _) => (member: context.TargetSymbol, attributes: context.Attributes, AttributeType.Localize))
        ];

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
                            switch (symbol)
                            {
                                case IFieldSymbol fieldSymbol:
                                    if (!NotifyPropertyGenerator.IsValidField(compilation, fieldSymbol))
                                    {
                                        continue;
                                    }
                                    break;
                                case IMethodSymbol methodSymbol:
                                    if (!NotifyPropertyGenerator.IsValidMethod(compilation, methodSymbol))
                                    {
                                        continue;
                                    }
                                    break;
                                default:
                                    continue;
                            }
                            if (!typeInfos.TryGetValue(symbol.ContainingType, out var typeInfo))
                            {
                                typeInfos[symbol.ContainingType] = typeInfo = [];
                            }
                            typeInfo.Add(item);
                            break;
                        case AttributeType.NotifyDataErrorInfo:
                            if (symbol is not INamedTypeSymbol namedTypeSymbol || !NotifyDataErrorInfoGenerator.IsValidType(compilation, namedTypeSymbol, attributes))
                            {
                                continue;
                            }
                            if (!typeInfos.TryGetValue(namedTypeSymbol, out typeInfo))
                            {
                                typeInfos[namedTypeSymbol] = typeInfo = [];
                            }
                            typeInfo.Add(item);
                            break;
                        case AttributeType.Localize:
                            if (symbol is not INamedTypeSymbol typeSymbol || !LocalizePropertyGenerator.IsValidType(typeSymbol, attributes, additionalTexts))
                            {
                                continue;
                            }
                            if (!typeInfos.TryGetValue(typeSymbol, out typeInfo))
                            {
                                typeInfos[typeSymbol] = typeInfo = [];
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

                    sb.Clear();
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

                    bool isFirst = true;
                    foreach (var group in members.GroupBy(m => m.attributeType))
                    {
                        switch (group.Key)
                        {
                            case AttributeType.Notify:
                                NotifyPropertyGenerator.Generate(writer, group.Select(m => m.member), compilation, ref isFirst);
                                break;

                            case AttributeType.NotifyDataErrorInfo:
                                NotifyDataErrorInfoGenerator.Generate(writer, group.Select(m => m.member), compilation, ref isFirst);
                                break;

                            case AttributeType.Localize:
                                LocalizePropertyGenerator.Generate(writer, group.Select(m => (m.member, m.attributes)), additionalTexts, ref isFirst);
                                break;

                            default:
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

                    context.AddSource(generatedFileName, sourceText);
                }// foreach (var pair in typeInfos)
            });
        }

        #endregion
    }
}
