using Microsoft.CodeAnalysis;
using System.Xml;

namespace Minimal.Mvvm.SourceGenerator
{
    public static class SymbolExtensions
    {
        private static readonly char[] s_xmlSplitChars = ['\n'];

        public static string[]? GetComment(this ISymbol symbol)
        {
            var commentXml = symbol.GetDocumentationCommentXml();
            if (string.IsNullOrEmpty(commentXml))
            {
                return null;
            }

            using var reader = XmlReader.Create(new StringReader(commentXml));

            string? content = null;
            while (reader.Read())
            {
                if (!reader.IsStartElement() || reader.Name != "member") continue;
                content = reader.ReadInnerXml();
                break;
            }

            if (string.IsNullOrWhiteSpace(content))
            {
                return null;
            }

            string[] lines = content!.Split(s_xmlSplitChars, StringSplitOptions.RemoveEmptyEntries);
            if (lines.Length == 0)
            {
                return null;
            }

            var (leadingWhitespace, leadingWhitespaceLength) = TextUtils.GetLeadingWhitespace(lines[0]);
            if (leadingWhitespaceLength == 0) return lines;

            for (int i = 0; i < lines.Length; i++)
            {
                if (lines[i].StartsWith(leadingWhitespace))
                {
                    lines[i] = lines[i].Substring(leadingWhitespaceLength);
                }
            }
            return lines;
        }

        public static bool InheritsFromType(this ITypeSymbol classSymbol, ITypeSymbol baseTypeSymbol)
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

        public static bool ImplementsInterface(this ITypeSymbol classSymbol, ITypeSymbol interfaceSymbol)
        {
            foreach (var implementedInterface in classSymbol.AllInterfaces)
            {
                if (SymbolEqualityComparer.Default.Equals(implementedInterface, interfaceSymbol))
                {
                    return true;
                }
            }
            return false;
        }

        public static bool InheritsFromTypeOrImplementsInterface(this ITypeSymbol classSymbol, ITypeSymbol typeSymbol)
        {
            return classSymbol.InheritsFromType(typeSymbol) || classSymbol.ImplementsInterface(typeSymbol);
        }

        public static bool IsAssignableFromType(this ITypeSymbol classSymbol, ITypeSymbol baseTypeSymbol)
        {
            return SymbolEqualityComparer.Default.Equals(classSymbol, baseTypeSymbol) 
                   || classSymbol.InheritsFromTypeOrImplementsInterface(baseTypeSymbol);
        }
    }
}
