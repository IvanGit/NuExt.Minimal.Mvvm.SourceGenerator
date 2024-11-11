using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.CodeDom.Compiler;
using System.Collections.Immutable;
using System.Diagnostics;

namespace Minimal.Mvvm.SourceGenerator
{
    internal struct NotifyPropertyGenerator
    {
        internal const string NotifyAttributeFullyQualifiedMetadataName = "Minimal.Mvvm.NotifyAttribute";
        internal const string BindableBaseFullyQualifiedMetadataName = "Minimal.Mvvm.BindableBase";

        private static readonly char[] s_trimChars = { '_' };

        private readonly record struct NotifyAttributeData(string? PropertyName, Accessibility PropertyAccessibility, Accessibility GetterAccessibility, Accessibility SetterAccessibility);

        #region Pipeline

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
            Debug.Assert(baseTypeSymbol != null);
            if (baseTypeSymbol == null || !containingType.InheritsFromType(baseTypeSymbol))
            {
                return false;
            }
            return true;
        }

        internal static bool Predicate(SyntaxNode attributeTarget, CancellationToken cancellationToken)
        {
            _ = cancellationToken;
            //Trace.WriteLine($"pipeline syntaxNode={attributeTarget}");
            bool result = attributeTarget is VariableDeclaratorSyntax { Parent: VariableDeclarationSyntax { Parent: FieldDeclarationSyntax
                    {
                        AttributeLists.Count: > 0, Parent: ClassDeclarationSyntax
                    }
                }
            };
            Debug.Assert(result);
            return result;
        }

        #endregion

        #region Methods

        public static void Generate(IndentedTextWriter writer, IEnumerable<(ISymbol member, ImmutableArray<AttributeData> attributes)> members)
        {
            bool isFirst = true;
            foreach (var (member, attributes) in members)
            {
                if (member is not IFieldSymbol fieldSymbol)
                {
                    Debug.Fail($"{member} is not a IFieldSymbol");
                    continue;
                }
                GenerateForMember(writer, fieldSymbol, attributes, ref isFirst);
            }
        }

        private static void GenerateForMember(IndentedTextWriter writer, IFieldSymbol fieldSymbol, ImmutableArray<AttributeData> attributes, ref bool isFirst)
        {
            if (fieldSymbol.IsReadOnly)
            {
                return;
            }

            var notifyAttribute = GetNotifyAttribute(attributes)!;
            var notifyAttributeData = GetNotifyAttributeData(notifyAttribute);

            var backingFieldName = fieldSymbol.Name;
            var propertyName = !string.IsNullOrWhiteSpace(notifyAttributeData.PropertyName) ? notifyAttributeData.PropertyName : GetPropertyName(backingFieldName);

            var fullyQualifiedTypeName = fieldSymbol.Type.ToDisplayString();

            if (!isFirst)
            {
                writer.WriteLine();
            }
            isFirst = false;

#if DEBUG
            //_writer.WriteLine($"//{fullyQualifiedTypeName} {fieldSymbol.ToDisplayString()}");
#endif
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
            writer.WriteLine($"set => SetProperty(ref {backingFieldName}, value);");

            writer.Indent--;
            writer.WriteLine("}");//end property
        }

        private static NotifyAttributeData GetNotifyAttributeData(AttributeData notifyAttribute)
        {
            string? propertyName = null;
            Accessibility propertyAccessibility, getterAccessibility = Accessibility.NotApplicable, setterAccessibility = Accessibility.NotApplicable;
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
                        case nameof(NotifyAttribute.Getter):
                            getterAccessibility = (Accessibility)typedConstant.Value!;
                            break;
                        case nameof(NotifyAttribute.Setter):
                            setterAccessibility = (Accessibility)typedConstant.Value!;
                            break;
                    }
                }
            }

            if (getterAccessibility == Accessibility.NotApplicable || getterAccessibility >= setterAccessibility)
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

            Debug.Assert(propertyAccessibility != Accessibility.NotApplicable);

            return new NotifyAttributeData(propertyName, propertyAccessibility, getterAccessibility,
                setterAccessibility);
        }

        private static AttributeData? GetNotifyAttribute(IEnumerable<AttributeData> attributes)
        {
            return attributes.SingleOrDefault(x => x.AttributeClass?.Name == "NotifyAttribute");
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
