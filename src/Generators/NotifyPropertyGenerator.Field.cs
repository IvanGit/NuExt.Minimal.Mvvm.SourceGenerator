using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.CodeDom.Compiler;

namespace Minimal.Mvvm.SourceGenerator
{
    partial struct NotifyPropertyGenerator
    {
        #region Methods

        private static bool IsValidVariableDeclarator(VariableDeclaratorSyntax variableDeclaratorSyntax)
        {
            return variableDeclaratorSyntax is
            {
                Parent: VariableDeclarationSyntax
                {
                    Parent: FieldDeclarationSyntax
                    {
                        AttributeLists.Count: > 0,
                        Parent: ClassDeclarationSyntax
                    }
                }
            };
        }

        internal static bool IsValidField(Compilation compilation, IFieldSymbol fieldSymbol)
        {
            return !fieldSymbol.IsReadOnly && IsValidContainingType(compilation, fieldSymbol.ContainingType);
        }

        private static void GenerateForField(IndentedTextWriter writer, IFieldSymbol fieldSymbol, NullableContextOptions nullableContextOptions, HashSet<string> propertyNames, bool useEventArgsCache, ref bool isFirst)
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

            var alsoNotifyAttributes = GetAlsoNotifyAttributes(attributes);
            var alsoNotifyAttributeData = GetAlsoNotifyAttributeData(alsoNotifyAttributes);

            var backingFieldName = fieldSymbol.Name;
            var propertyName = !string.IsNullOrWhiteSpace(notifyAttributeData.PropertyName) ? notifyAttributeData.PropertyName! : GetPropertyNameFromFieldName(backingFieldName);

            var propertyType = fieldSymbol.Type;

            var fullyQualifiedTypeName = propertyType.ToDisplayString(SymbolDisplayFormats.FullyQualifiedTypeName);

            var callbackData = GetCallbackData(fieldSymbol.ContainingType, propertyType, notifyAttributeData);

            string nullable = nullableContextOptions.HasFlag(NullableContextOptions.Annotations) ? "?" : "";

            GenerateProperty(writer, propertyName, backingFieldName, fullyQualifiedTypeName, notifyAttributeData, callbackData, customAttributeData, alsoNotifyAttributeData, comment, nullable, false, useEventArgsCache, ref isFirst);

            if (useEventArgsCache)
            {
                propertyNames.Add(propertyName);
            }
        }

        private static string GetPropertyNameFromFieldName(string backingFieldName)
        {
            var propertyName = backingFieldName;
            if (propertyName.StartsWith("_"))
            {
                propertyName = propertyName.TrimStart(s_trimChars);
            }
            propertyName = char.ToUpper(propertyName[0]) + propertyName.Substring(1);
            return propertyName;
        }

        #endregion
    }
}
