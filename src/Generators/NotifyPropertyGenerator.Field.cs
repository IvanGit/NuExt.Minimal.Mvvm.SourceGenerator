using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

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

        private static void GenerateForField(scoped NotifyPropertyGeneratorContext ctx, IFieldSymbol fieldSymbol, ref bool isFirst)
        {
            if (fieldSymbol.IsReadOnly)
            {
                return;
            }

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

            var propCtx = new NotifyPropertyContext(notifyAttributeData, callbackData, customAttributeData, alsoNotifyAttributeData, fieldSymbol.GetComment(), fullyQualifiedTypeName, propertyName, backingFieldName, false);

            GenerateProperty(ctx, propCtx, ref isFirst);
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
