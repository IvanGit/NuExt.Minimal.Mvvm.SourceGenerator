using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.CodeDom.Compiler;
using System.Collections.Immutable;
using System.Diagnostics;

namespace Minimal.Mvvm.SourceGenerator
{
    internal partial struct NotifyPropertyGenerator
    {
        internal const string BindableBaseFullyQualifiedName = "Minimal.Mvvm.BindableBase";
        internal const string CustomAttributeFullyQualifiedName = "global::Minimal.Mvvm.CustomAttributeAttribute";
        internal const string NotifyAttributeFullyQualifiedName = "Minimal.Mvvm.NotifyAttribute";

        private static readonly char[] s_trimChars = ['_'];

        private readonly record struct CallbackData(string? CallbackName, bool HasParameter);

        private readonly record struct CustomAttributeData(string Attribute);

        private readonly record struct NotifyAttributeData(string? PropertyName, string? CallbackName, bool? PreferCallbackWithParameter, Accessibility PropertyAccessibility, Accessibility GetterAccessibility, Accessibility SetterAccessibility);


        #region Pipeline

        internal static bool IsValidSyntaxNode(SyntaxNode attributeTarget, CancellationToken cancellationToken)
        {
            _ = cancellationToken;
            //Trace.WriteLine($"pipeline syntaxNode={attributeTarget}");
            return attributeTarget switch
            {
                VariableDeclaratorSyntax v => IsValidVariableDeclarator(v),
                MethodDeclarationSyntax m => IsValidMethodDeclaration(m),
                _ => false,
            };
        }

        #endregion

        #region Methods

        private static bool IsValidContainingType(Compilation compilation, ITypeSymbol containingType)
        {
            var baseTypeSymbol = compilation.GetTypeByMetadataName(RemoveGlobalAlias(BindableBaseFullyQualifiedName));
            return baseTypeSymbol != null && containingType.InheritsFromType(baseTypeSymbol);
        }

        public static void Generate(IndentedTextWriter writer, IEnumerable<ISymbol> members, Compilation compilation)
        {
            var nullableContextOptions = compilation.Options.NullableContextOptions;
            bool isFirst = true;
            foreach (var member in members)
            {
                switch (member)
                {
                    case IFieldSymbol fieldSymbol:
                        GenerateForField(writer, fieldSymbol, nullableContextOptions, ref isFirst);
                        break;
                    case IMethodSymbol methodSymbol:
                        GenerateForMethod(writer, methodSymbol, compilation, nullableContextOptions, ref isFirst);
                        break;
                    default:
                        break;
                }
            }
        }

        private static void GenerateProperty(IndentedTextWriter writer, string propertyName, string backingFieldName,
            string fullyQualifiedTypeName, NotifyAttributeData notifyAttributeData, CallbackData callbackData,
            IEnumerable<CustomAttributeData> customAttributeData, string[]? comment, string nullable, bool generateBackingFieldName, ref bool isFirst)
        {
            if (!isFirst)
            {
                writer.WriteLineNoTabs(string.Empty);
            }
            isFirst = false;

            string? backingCallbackFieldName = null;
            if (callbackData.CallbackName != null)
            {
                backingCallbackFieldName = $"{backingFieldName}ChangedCallback";
                writer.Write("private global::System.Action");
                if (callbackData.HasParameter)
                {
                    writer.Write($"<{fullyQualifiedTypeName}>");
                }
                writer.WriteLine($"{nullable} {backingCallbackFieldName};");
                writer.WriteLineNoTabs(string.Empty);
            }

            if (generateBackingFieldName)
            {
                writer.WriteLine($"private {fullyQualifiedTypeName} {backingFieldName};");
            }

            if (comment != null)
            {
                foreach (string line in comment)
                {
                    writer.WriteLine($"/// {line}");
                }
            }

            foreach (var customAttribute in customAttributeData)
            {
                writer.WriteLine(customAttribute.Attribute);
            }

            writer.WriteAccessibility(notifyAttributeData.PropertyAccessibility);
            /*if (notifyAttributeData.IsVirtual)
            {
                writer.Write("virtual ");
            }*/
            writer.WriteLine($"{fullyQualifiedTypeName} {propertyName}");
            writer.WriteLine("{"); //begin property
            writer.Indent++;

            writer.WriteAccessibility(notifyAttributeData.GetterAccessibility);
            writer.WriteLine($"get => {backingFieldName};");

            writer.WriteAccessibility(notifyAttributeData.SetterAccessibility);
            writer.WriteLine(callbackData.CallbackName != null
                ? $"set => SetProperty(ref {backingFieldName}, value, {backingCallbackFieldName} ??= {callbackData.CallbackName});"
                : $"set => SetProperty(ref {backingFieldName}, value);");

            writer.Indent--;
            writer.WriteLine("}"); //end property
        }

        private static CallbackData GetCallbackData(INamedTypeSymbol containingType, ITypeSymbol? parameterType, NotifyAttributeData notifyAttributeData)
        {
            if (notifyAttributeData.CallbackName == null) return default;

            var members = containingType.GetMembers(notifyAttributeData.CallbackName);
            if (members.Length == 0)
            {
                return new CallbackData(notifyAttributeData.CallbackName, false);
            }

            var methods = new List<(IMethodSymbol method, bool hasParameter)>(members.Length);

            bool hasParameter;
            foreach (var member in members)
            {
                if (member is not IMethodSymbol method || !IsCallback(method, parameterType, out hasParameter))
                {
                    continue;
                }
                methods.Add((method, hasParameter));
            }

            hasParameter = methods.Count switch
            {
                1 => methods[0].hasParameter,
                > 1 when notifyAttributeData.PreferCallbackWithParameter == true => methods.Any(m => m.hasParameter),
                > 1 => methods.All(m => m.hasParameter),
                _ => false
            };
            return new CallbackData(notifyAttributeData.CallbackName, hasParameter);
        }

        private static bool IsCallback(IMethodSymbol methodSymbol, ITypeSymbol? parameterType, out bool hasParameter)
        {
            hasParameter = false;
            if (!methodSymbol.ReturnsVoid) return false;
            var parameters = methodSymbol.Parameters;
            if (parameters.Length > 1) return false;
            hasParameter = parameters.Length == 1;
            return parameters.Length == 0 || parameterType?.IsAssignableFromType(parameters[0].Type) == true;
        }

        private static AttributeData? GetNotifyAttribute(IEnumerable<AttributeData> attributes)
        {
            return attributes.SingleOrDefault(x => x.AttributeClass?.Name == "NotifyAttribute");
        }

        private static NotifyAttributeData GetNotifyAttributeData(AttributeData notifyAttribute)
        {
            string? propertyName = null, callbackName = null;
            bool? preferCallbackWithParameter = null;
            Accessibility propertyAccessibility, getterAccessibility = Accessibility.NotApplicable, setterAccessibility = Accessibility.NotApplicable;
            if (notifyAttribute.ConstructorArguments.Length > 0)
            {
                foreach (var typedConstant in notifyAttribute.ConstructorArguments)
                {
                    switch (typedConstant.Type?.SpecialType)
                    {
                        case SpecialType.System_String:
                            propertyName = (string?)typedConstant.Value;
                            break;
                    }
                }
            }
            if (notifyAttribute.NamedArguments.Length > 0)
            {
                foreach (var pair in notifyAttribute.NamedArguments)
                {
                    var name = pair.Key;
                    var typedConstant = pair.Value;
                    switch (name)
                    {
                        case nameof(NotifyAttribute.PropertyName):
                            propertyName = (string?)typedConstant.Value;
                            break;
                        case nameof(NotifyAttribute.CallbackName):
                            callbackName = (string?)typedConstant.Value;
                            break;
                        case nameof(NotifyAttribute.PreferCallbackWithParameter):
                            preferCallbackWithParameter = (bool?)typedConstant.Value;
                            break;
                        case nameof(NotifyAttribute.Getter):
                            getterAccessibility = (Accessibility)typedConstant.Value!;
                            break;
                        case nameof(NotifyAttribute.Setter):
                            setterAccessibility = (Accessibility)typedConstant.Value!;
                            break;
                        default:
                            Trace.WriteLine($"Unexpected argument name: {name}");
                            break;
                    }
                }
            }

            if (getterAccessibility == Accessibility.Internal && setterAccessibility == Accessibility.Protected ||
                getterAccessibility == Accessibility.Protected && setterAccessibility == Accessibility.Internal)
            {
                //1) get is internal, set is protected OR 2) get is protected, set is internal
                propertyAccessibility = Accessibility.ProtectedOrInternal;
                getterAccessibility = Accessibility.NotApplicable;
            }
            else if (getterAccessibility == Accessibility.NotApplicable || getterAccessibility >= setterAccessibility)
            {
                propertyAccessibility = getterAccessibility == Accessibility.NotApplicable ? Accessibility.Public : getterAccessibility;
                if (getterAccessibility != Accessibility.NotApplicable && getterAccessibility == setterAccessibility)
                {
                    setterAccessibility = Accessibility.NotApplicable;
                }
                getterAccessibility = Accessibility.NotApplicable;
            }
            else
            {
                propertyAccessibility = setterAccessibility;
                setterAccessibility = Accessibility.NotApplicable;
            }

            return new NotifyAttributeData(propertyName, callbackName, preferCallbackWithParameter, propertyAccessibility, getterAccessibility, setterAccessibility);
        }

        private static IEnumerable<AttributeData> GetCustomAttributes(ImmutableArray<AttributeData> attributes)
        {
            return attributes.Where(x => x.AttributeClass?.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat) == CustomAttributeFullyQualifiedName);
        }

        private static IEnumerable<CustomAttributeData> GetCustomAttributeData(IEnumerable<AttributeData> customAttributes)
        {
            List<CustomAttributeData>? list = null;
            foreach (var customAttribute in customAttributes)
            {
                if (customAttribute.ConstructorArguments.Length <= 0) continue;
                foreach (var typedConstant in customAttribute.ConstructorArguments)
                {
                    switch (typedConstant.Type?.SpecialType)
                    {
                        case SpecialType.System_String:
                            var attribute = (string?)typedConstant.Value;
                            if (!string.IsNullOrWhiteSpace(attribute))
                            {
                                attribute = attribute!.Trim();
                                if (!attribute.StartsWith("[") && !attribute.EndsWith("]"))
                                {
                                    attribute = $"[{attribute}]";
                                }
                                list ??= [];
                                list.Add(new CustomAttributeData(attribute));
                            }
                            break;
                    }
                }
            }

            return (IEnumerable<CustomAttributeData>?)list ?? [];
        }

        private static string RemoveGlobalAlias(string fullyQualifiedMetadataName)
        {
            return fullyQualifiedMetadataName.StartsWith("global::") ? fullyQualifiedMetadataName.Substring("global::".Length) : fullyQualifiedMetadataName;
        }

        #endregion
    }
}
