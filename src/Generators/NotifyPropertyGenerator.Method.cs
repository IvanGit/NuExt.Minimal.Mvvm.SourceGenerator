using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Minimal.Mvvm.SourceGenerator
{
    partial struct NotifyPropertyGenerator
    {
        #region Methods

        private static bool IsValidMethodDeclaration(MethodDeclarationSyntax methodDeclarationSyntax)
        {
            if (methodDeclarationSyntax is not
                {
                    Parent: ClassDeclarationSyntax,
                    ParameterList.Parameters.Count: <= 1,
                })
            {
                return false;
            }
            return methodDeclarationSyntax.ReturnType switch
            {
                IdentifierNameSyntax identifierNameSyntax => identifierNameSyntax.Identifier is
                {
                    ValueText: "Task"
                },
                PredefinedTypeSyntax predefinedTypeSyntax => predefinedTypeSyntax.Keyword.IsKind(SyntaxKind.VoidKeyword),
                QualifiedNameSyntax qualifiedNameSyntax => qualifiedNameSyntax.ToString() == "global::System.Threading.Tasks.Task" || qualifiedNameSyntax.ToString() == "System.Threading.Tasks.Task",
                _ => false,
            };
        }

        internal static bool IsValidMethod(Compilation compilation, IMethodSymbol methodSymbol)
        {
            if (!IsValidContainingType(compilation, methodSymbol.ContainingType))
            {
                return false;
            }
            if (!methodSymbol.ReturnsVoid &&
                methodSymbol.ReturnType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat) !=
                "global::System.Threading.Tasks.Task")
            {
                return false;
            }
            return methodSymbol.Parameters.Length <= 1;
        }

        private static void GenerateForMethod(scoped NotifyPropertyGeneratorContext ctx, IMethodSymbol methodSymbol, ref bool isFirst)
        {
            var attributes = methodSymbol.GetAttributes();

            var notifyAttribute = GetNotifyAttribute(attributes)!;
            var notifyAttributeData = GetNotifyAttributeData(notifyAttribute);

            var customAttributes = GetCustomAttributes(attributes);
            var customAttributeData = GetCustomAttributeData(customAttributes);

            var alsoNotifyAttributes = GetAlsoNotifyAttributes(attributes);
            var alsoNotifyAttributeData = GetAlsoNotifyAttributeData(alsoNotifyAttributes);

            var propertyName = !string.IsNullOrWhiteSpace(notifyAttributeData.PropertyName) ? notifyAttributeData.PropertyName! : GetPropertyNameFromMethodName(methodSymbol.Name);

            var backingFieldName = GetBackingFieldNameFromPropertyName(propertyName);

            var propertyType = GetCommandTypeName(ctx.Compilation, methodSymbol);

            var callbackData = GetCallbackData(methodSymbol.ContainingType, propertyType, notifyAttributeData);

            string nullable = ctx.Compilation.Options.NullableContextOptions.HasFlag(NullableContextOptions.Annotations) ? "?" : "";
            var fullyQualifiedTypeName = $"{propertyType?.ToDisplayString(SymbolDisplayFormats.FullyQualifiedTypeName)}{nullable}";

            var propCtx = new NotifyPropertyContext(notifyAttributeData, callbackData, customAttributeData, alsoNotifyAttributeData, methodSymbol.GetComment(), fullyQualifiedTypeName, propertyName, backingFieldName, true);

            GenerateProperty(ctx, propCtx, ref isFirst);
        }

        private static INamedTypeSymbol? GetCommandTypeName(Compilation compilation, IMethodSymbol methodSymbol)
        {
            var parameters = methodSymbol.Parameters;
            if (parameters.Length == 0)
            {
                return compilation.GetTypeByMetadataName(methodSymbol.ReturnsVoid ? "System.Windows.Input.ICommand" : "Minimal.Mvvm.IAsyncCommand");
            }

            var genericCommandType = compilation.GetTypeByMetadataName(methodSymbol.ReturnsVoid ? "Minimal.Mvvm.ICommand`1" : "Minimal.Mvvm.IAsyncCommand`1");

            if (genericCommandType != null)
            {
                var commandType = genericCommandType.Construct(parameters[0].Type);
                return commandType;
            }

            return null;
        }

        private static string GetPropertyNameFromMethodName(string methodName)
        {
            var propertyName = methodName;
            if (propertyName.EndsWith("Async"))
            {
                propertyName = propertyName.Substring(0, propertyName.Length - "Async".Length);
            }
            propertyName = char.ToUpper(propertyName[0]) + propertyName.Substring(1) + "Command";
            return propertyName;
        }

        private static string GetBackingFieldNameFromPropertyName(string propertyName)
        {
            var backingFieldName = "_" + char.ToLower(propertyName[0]) + propertyName.Substring(1);
            return backingFieldName;
        }

        #endregion
    }
}
