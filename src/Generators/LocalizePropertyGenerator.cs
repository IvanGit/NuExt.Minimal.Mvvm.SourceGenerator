using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Newtonsoft.Json;
using System.CodeDom.Compiler;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Xml.Linq;

namespace Minimal.Mvvm.SourceGenerator
{
    internal struct LocalizePropertyGenerator
    {
        internal const string LocalizeAttributeFullyQualifiedName = "Minimal.Mvvm.LocalizeAttribute";

        private readonly record struct LocalizeAttributeData(string? JsonFileName);

        #region Pipeline

        internal static bool IsValidSyntaxNode(SyntaxNode attributeTarget, CancellationToken cancellationToken)
        {
            _ = cancellationToken;
            //Trace.WriteLine($"pipeline syntaxNode={attributeTarget}");
            return attributeTarget is ClassDeclarationSyntax;
        }

        public static bool IsValidType(ITypeSymbol typeSymbol,
            ImmutableArray<AttributeData> attributes,
            ImmutableArray<(string name, AdditionalText text)> additionalTexts)
        {
            _ = typeSymbol;
            var localizeAttribute = GetLocalizeAttribute(attributes);
            if (localizeAttribute == null)
            {
                return false;
            }
            var localizeAttributeData = GetLocalizeAttributeData(localizeAttribute);
            if (string.IsNullOrEmpty(localizeAttributeData.JsonFileName))
            {
                return false;
            }
            var jsonFileName = Path.GetFileName(localizeAttributeData.JsonFileName);

            return additionalTexts.Any(pair => pair.name == jsonFileName);
        }

        #endregion

        #region Methods

        public static void Generate(IndentedTextWriter writer, IEnumerable<(ISymbol member, ImmutableArray<AttributeData> attributes)> members, ImmutableArray<(string name, AdditionalText text)> additionalTexts)
        {
            foreach (var (member, attributes) in members)
            {
                if (member is not ITypeSymbol typeSymbol)
                {
                    Trace.WriteLine($"{member} is not a ITypeSymbol");
                    continue;
                }
                GenerateForMember(writer, typeSymbol, attributes, additionalTexts);
            }
        }

        private static void GenerateForMember(IndentedTextWriter writer, ITypeSymbol typeSymbol,
            ImmutableArray<AttributeData> attributes,
            ImmutableArray<(string name, AdditionalText text)> additionalTexts)
        {
            _ = typeSymbol;
            var localizeAttribute = GetLocalizeAttribute(attributes)!;
            var localizeAttributeData = GetLocalizeAttributeData(localizeAttribute);
            var jsonFileName = Path.GetFileName(localizeAttributeData.JsonFileName);

            var text = additionalTexts.First(pair => pair.name == jsonFileName).text;

            Dictionary<string, string>? translations;
            try
            {
                translations = JsonConvert.DeserializeObject<Dictionary<string, string>>(text.GetText()!.ToString());
            }
            catch (Exception ex)
            {
                Trace.TraceError($"Exception while deserializing '{jsonFileName}': {ex.Message}");
                return;
            }

            if (translations == null || translations.Count == 0)
            {
                return;
            }

            bool isFirst = true;
            foreach (var pair in translations)
            {
                if (!isFirst)
                {
                    writer.WriteLineNoTabs(string.Empty);
                }
                isFirst = false;
                writer.WriteLine("/// <summary>");
                writer.WriteLine($"/// Looks up a localized string similar to {EscapeString(pair.Value)}.");
                writer.WriteLine("/// </summary>");
                writer.WriteLine($"public static string {StringToValidPropertyName(pair.Key)} {{ get; set; }} = {JsonConvert.ToString(pair.Value)};");
            }
        }

        private static string EscapeString(string value)
        {
            var s = new XElement("t", value).LastNode.ToString();
            s = s
                //.Replace("\"", "&quot;")
                //.Replace("'", "&apos;")
                .Replace("\r\n", "\\r\\n")
                .Replace("\r", "\\r")
                .Replace("\n", "\\n");
            return s;
        }

        private static string StringToValidPropertyName(string key)
        {
            var s = key.Trim();
            var validName = char.IsLetter(s[0]) ? char.ToUpper(s[0]).ToString() : "_";
            validName += new string(s.Skip(1).Select(ch => char.IsLetterOrDigit(ch) ? ch : '_').ToArray());
            return validName;
        }

        private static AttributeData? GetLocalizeAttribute(ImmutableArray<AttributeData> attributes)
        {
            return attributes.SingleOrDefault(x => x.AttributeClass?.Name == "LocalizeAttribute");
        }

        private static LocalizeAttributeData GetLocalizeAttributeData(AttributeData localizeAttribute)
        {
            string? jsonFileName = null;
            if (localizeAttribute.ConstructorArguments.Length > 0)
            {
                foreach (var typedConstant in localizeAttribute.ConstructorArguments)
                {
                    switch (typedConstant.Type?.SpecialType)
                    {
                        case SpecialType.System_String:
                            jsonFileName = (string?)typedConstant.Value;
                            break;
                    }
                }
            }
            return new LocalizeAttributeData(jsonFileName);
        }

        #endregion
    }
}
