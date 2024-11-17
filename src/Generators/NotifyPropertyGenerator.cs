using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.CodeDom.Compiler;
using System.Collections.Immutable;
using System.Diagnostics;

namespace Minimal.Mvvm.SourceGenerator
{
    internal struct NotifyPropertyGenerator
    {
        internal const string BindableBaseFullyQualifiedMetadataName = "Minimal.Mvvm.BindableBase";
        internal const string CustomAttributeFullyQualifiedMetadataName = "Minimal.Mvvm.CustomAttributeAttribute";
        internal const string NotifyAttributeFullyQualifiedMetadataName = "Minimal.Mvvm.NotifyAttribute";

        private static readonly char[] s_trimChars = { '_' };

        private readonly record struct CallbackData(string? CallbackName, bool HasParameter);

        private readonly record struct CustomAttributeData(string Attribute);

        private readonly record struct NotifyAttributeData(string? PropertyName, string? CallbackName, bool? PreferCallbackWithParameter, Accessibility PropertyAccessibility, Accessibility GetterAccessibility, Accessibility SetterAccessibility);


        #region Pipeline

        internal static bool Predicate(SyntaxNode attributeTarget, CancellationToken cancellationToken)
        {
            _ = cancellationToken;
            //Trace.WriteLine($"pipeline syntaxNode={attributeTarget}");
            bool result = attributeTarget is VariableDeclaratorSyntax
            {
                Parent: VariableDeclarationSyntax
                {
                    Parent: FieldDeclarationSyntax
                    {
                        AttributeLists.Count: > 0, Parent: ClassDeclarationSyntax
                    }
                }
            };
            return result;
        }

        internal static bool Predicate(Compilation compilation, IFieldSymbol fieldSymbol)
        {
            if (fieldSymbol.IsReadOnly)
            {
                return false;
            }
            var containingType = fieldSymbol.ContainingType;
            if (containingType == null)
            {
                return false;
            }
            var baseTypeSymbol = compilation.GetTypeByMetadataName(BindableBaseFullyQualifiedMetadataName);
            if (baseTypeSymbol == null || !containingType.InheritsFromType(baseTypeSymbol))
            {
                return false;
            }
            return true;
        }


        #endregion

        #region Methods

        public static void Generate(IndentedTextWriter writer, IEnumerable<ISymbol> members, NullableContextOptions nullableContextOptions)
        {
            bool isFirst = true;
            foreach (var member in members)
            {
                if (member is not IFieldSymbol fieldSymbol)
                {
                    Trace.WriteLine($"{member} is not a IFieldSymbol");
                    continue;
                }
                GenerateForMember(writer, fieldSymbol, nullableContextOptions, ref isFirst);
            }
        }

        private static void GenerateForMember(IndentedTextWriter writer, IFieldSymbol fieldSymbol, NullableContextOptions nullableContextOptions, ref bool isFirst)
        {
            if (fieldSymbol.IsReadOnly)
            {
                return;
            }

            var comment = fieldSymbol.GetComment();

            var attributes = fieldSymbol.GetAttributes();

            var notifyAttribute = GetNotifyAttribute(attributes)!;
            var notifyAttributeData = GetNotifyAttributeData(notifyAttribute);

            var customAttributes = GetCustomAttributes(attributes);
            var customAttributeData = GetCustomAttributeData(customAttributes);

            var backingFieldName = fieldSymbol.Name;
            var propertyName = !string.IsNullOrWhiteSpace(notifyAttributeData.PropertyName) ? notifyAttributeData.PropertyName : GetPropertyName(backingFieldName);

            var fullyQualifiedTypeName = fieldSymbol.Type.ToDisplayString(SymbolDisplayFormats.FullyQualifiedTypeName);

            var callbackData = GetCallbackData(fieldSymbol, fullyQualifiedTypeName, notifyAttributeData);

            if (!isFirst)
            {
                writer.WriteLineNoTabs(string.Empty);
            }
            isFirst = false;

            string nullable = nullableContextOptions.HasFlag(NullableContextOptions.Annotations) ? "?" : "";

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
            writer.WriteLine("{");//begin property
            writer.Indent++;

            writer.WriteAccessibility(notifyAttributeData.GetterAccessibility);
            writer.WriteLine($"get => {backingFieldName};");

            writer.WriteAccessibility(notifyAttributeData.SetterAccessibility);
            writer.WriteLine(callbackData.CallbackName != null
                ? $"set => SetProperty(ref {backingFieldName}, value, {backingCallbackFieldName} ??= {callbackData.CallbackName});"
                : $"set => SetProperty(ref {backingFieldName}, value);");

            writer.Indent--;
            writer.WriteLine("}");//end property
        }

        private static CallbackData GetCallbackData(IFieldSymbol fieldSymbol, string fullyQualifiedTypeName,
            NotifyAttributeData notifyAttributeData)
        {
            if (notifyAttributeData.CallbackName == null) return default;

            var containingType = fieldSymbol.ContainingType;
            var members = containingType.GetMembers(notifyAttributeData.CallbackName);
            if (members.Length == 0)
            {
                return new CallbackData(notifyAttributeData.CallbackName, false);
            }

            var methods = new List<(IMethodSymbol method, bool hasParameter)>(members.Length);

            bool hasParameter;
            foreach (var member in members)
            {
                if (member is not IMethodSymbol method || !IsCallback(method, fieldSymbol.Type, out hasParameter))
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

        private static bool IsCallback(IMethodSymbol methodSymbol, ITypeSymbol parameterType, out bool hasParameter)
        {
            hasParameter = false;
            if (!methodSymbol.ReturnsVoid) return false;
            var parameters = methodSymbol.Parameters;
            if (parameters.Length > 1) return false;
            hasParameter = parameters.Length == 1;
            return parameters.Length == 0 || parameterType.IsAssignableFromType(parameters[0].Type);
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

            return new NotifyAttributeData(propertyName, callbackName, preferCallbackWithParameter, propertyAccessibility, getterAccessibility,
                setterAccessibility);
        }

        private static IEnumerable<AttributeData> GetCustomAttributes(ImmutableArray<AttributeData> attributes)
        {
            return attributes.Where(x => x.AttributeClass?.ToDisplayString() ==
                                         CustomAttributeFullyQualifiedMetadataName);
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
                                list ??= new List<CustomAttributeData>();
                                list.Add(new CustomAttributeData(attribute));
                            }
                            break;
                    }
                }
            }

            return (IEnumerable<CustomAttributeData>?)list ?? Array.Empty<CustomAttributeData>();
        }

        private static string GetPropertyName(string fieldName)
        {
            var newFieldName = fieldName;
            if (newFieldName.StartsWith("_"))
            {
                newFieldName = newFieldName.TrimStart(s_trimChars);
            }
            newFieldName = char.ToUpper(newFieldName[0]) + newFieldName.Substring(1);
            return newFieldName;
        }

        #endregion
    }
}
