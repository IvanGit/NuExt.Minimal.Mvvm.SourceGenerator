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
            var alsoNotifyAttributes = GetAlsoNotifyAttributes(attributes);
            var customAttributes = GetCustomAttributes(attributes);
            var useCommandManagerAttribute = GetUseCommandManagerAttribute(attributes);

            var notifyAttributeData = GetNotifyAttributeData(notifyAttribute);
            var alsoNotifyAttributeData = GetAlsoNotifyAttributeData(alsoNotifyAttributes);
            var customAttributeData = GetCustomAttributeData(customAttributes);
            var useCommandManagerAttributeData = GetUseCommandManagerAttributeData(useCommandManagerAttribute);

            var backingFieldName = fieldSymbol.Name;
            var propertyName = !string.IsNullOrWhiteSpace(notifyAttributeData.PropertyName) ? notifyAttributeData.PropertyName! : GetPropertyNameFromFieldName(backingFieldName);

            var propertyType = fieldSymbol.Type;

            bool isCommand = GetIsCommand(ctx.Compilation, propertyType);

            var fullyQualifiedTypeName = propertyType.ToDisplayString(SymbolDisplayFormats.FullyQualifiedTypeName);

            var callbackData = GetCallbackData(fieldSymbol.ContainingType, propertyType, notifyAttributeData);

            var propCtx = new NotifyPropertyContext(notifyAttributeData, callbackData, customAttributeData, alsoNotifyAttributeData,
                useCommandManagerAttributeData, isCommand, fieldSymbol.GetComment(), fullyQualifiedTypeName, propertyName, backingFieldName, false);

            GenerateProperty(ctx, propCtx, ref isFirst);
        }

        private static bool GetIsCommand(Compilation compilation, ITypeSymbol? propertyType)
        {
            if (propertyType == null) return false;
            var baseTypeSymbol = compilation.GetTypeByMetadataName("Minimal.Mvvm.IRelayCommand");
            if (baseTypeSymbol != null && propertyType.IsAssignableFromType(baseTypeSymbol))
                return true;
            return false;
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
