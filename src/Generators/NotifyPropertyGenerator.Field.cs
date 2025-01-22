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

            ctx.Comment = fieldSymbol.GetComment();
            var attributes = fieldSymbol.GetAttributes();

            var notifyAttribute = GetNotifyAttribute(attributes)!;
            var notifyAttributeData = GetNotifyAttributeData(notifyAttribute);

            var customAttributes = GetCustomAttributes(attributes);
            var customAttributeData = GetCustomAttributeData(customAttributes);

            var alsoNotifyAttributes = GetAlsoNotifyAttributes(attributes);
            var alsoNotifyAttributeData = GetAlsoNotifyAttributeData(alsoNotifyAttributes);

            ctx.BackingFieldName = fieldSymbol.Name;
            ctx.PropertyName = !string.IsNullOrWhiteSpace(notifyAttributeData.PropertyName) ? notifyAttributeData.PropertyName! : GetPropertyNameFromFieldName(ctx.BackingFieldName);

            var propertyType = fieldSymbol.Type;

            ctx.FullyQualifiedTypeName = propertyType.ToDisplayString(SymbolDisplayFormats.FullyQualifiedTypeName);

            var callbackData = GetCallbackData(fieldSymbol.ContainingType, propertyType, notifyAttributeData);

            ctx.GenerateBackingFieldName = false;

            GenerateProperty(ctx, notifyAttributeData, callbackData, customAttributeData, alsoNotifyAttributeData, ref isFirst);
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
