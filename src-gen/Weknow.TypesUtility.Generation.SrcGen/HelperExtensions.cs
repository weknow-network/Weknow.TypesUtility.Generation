using System.Text.RegularExpressions;
using System.Text;
using Microsoft.CodeAnalysis;
using System.Collections.Immutable;

namespace Weknow.TypesUtility;

internal static class HelperExtensions
{
    internal const string TARGET_ATTRIBUTE = "NullableShadowAttribute";
    internal static readonly string TARGET_SHORT_ATTRIBUTE = "NullableShadow";
    internal const string SUFFIX_DEFAULT = "Nullable";
    internal const string SUFFIX_START = "Suffix";
    internal static readonly Regex SUFFIX_CONVENSION = new Regex(@"Suffix\s*=\s*""(.*)""");

    public static string GetTypeName(this ITypeSymbol type)
    {
        var parts = type.ToDisplayParts(SymbolDisplayFormat.MinimallyQualifiedFormat);
        var formatted = parts.Select(m => GetName(m)); 
       
        string result = string.Join("", formatted);
        return result;

        string GetName(SymbolDisplayPart part)
        {
            if (!(part.Symbol is ITypeSymbol symbol))
            {
                return part.ToString();
            }

            AttributeData? att = symbol?.GetAttributes().FirstOrDefault(m =>
                                       m.AttributeClass?.Name == TARGET_ATTRIBUTE ||
                                       m.AttributeClass?.Name == TARGET_SHORT_ATTRIBUTE);

            if (att == null)
                return part.ToString();

            KeyValuePair<string, TypedConstant>? prop = att.NamedArguments.FirstOrDefault(m => m.Key.StartsWith(SUFFIX_START));

            if (prop?.Key == null)
                return $"{symbol}{SUFFIX_DEFAULT}";
            else
                return $"{symbol}{prop.Value}";
        }
    }

}
