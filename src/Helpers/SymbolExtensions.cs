using Microsoft.CodeAnalysis;

namespace Minimal.Mvvm.SourceGenerator
{
    public static class SymbolExtensions
    {
        public static bool InheritsFromType(this INamedTypeSymbol classSymbol, INamedTypeSymbol baseTypeSymbol)
        {
            var currentSymbol = classSymbol.BaseType;
            while (currentSymbol != null)
            {
                if (SymbolEqualityComparer.Default.Equals(currentSymbol, baseTypeSymbol))
                {
                    return true;
                }
                currentSymbol = currentSymbol.BaseType;
            }
            return false;
        }
    }
}
