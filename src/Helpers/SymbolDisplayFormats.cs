using Microsoft.CodeAnalysis;

namespace Minimal.Mvvm.SourceGenerator
{
    internal static class SymbolDisplayFormats
    {
        internal static SymbolDisplayFormat FullyQualifiedTypeName = SymbolDisplayFormat.FullyQualifiedFormat
            .WithMiscellaneousOptions(SymbolDisplayMiscellaneousOptions.EscapeKeywordIdentifiers
                                      | SymbolDisplayMiscellaneousOptions.UseSpecialTypes
                                      | SymbolDisplayMiscellaneousOptions.IncludeNullableReferenceTypeModifier);

        internal static SymbolDisplayFormat GeneratedFileName = new(
            globalNamespaceStyle: SymbolDisplayGlobalNamespaceStyle.Omitted, 
            typeQualificationStyle: SymbolDisplayTypeQualificationStyle.NameAndContainingTypesAndNamespaces);

        internal static SymbolDisplayFormat Namespace = SymbolDisplayFormat.FullyQualifiedFormat
            .WithGlobalNamespaceStyle(SymbolDisplayGlobalNamespaceStyle.Omitted);

        internal static SymbolDisplayFormat TypeDeclaration = new (
            genericsOptions: SymbolDisplayGenericsOptions.IncludeTypeParameters,
            kindOptions: SymbolDisplayKindOptions.IncludeTypeKeyword,
            miscellaneousOptions: SymbolDisplayMiscellaneousOptions.EscapeKeywordIdentifiers |
                                  SymbolDisplayMiscellaneousOptions.UseSpecialTypes);
    }
}
