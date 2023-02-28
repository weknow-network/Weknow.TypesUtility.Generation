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

    public static string GetTypeName(this ITypeSymbol type, bool toNullable = true)
    {
        if (!toNullable)
            return type.ToDisplayString();

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
            {
                string res = $"{part}{SUFFIX_DEFAULT}";
                return res;
            }
            else
                return $"{part}{prop.Value}";
        }
    }

    public static string CastExplicit(this ISymbol symbol, string statement, bool toNullable = false)
    {
        if (!(symbol is ITypeSymbol ts))
            return $"({symbol.ToDisplayString()}){statement}";

        (CollectionType colType, string itemType) = ts.GetCollectionType(toNullable);
        string result = colType switch
        {
            CollectionType.Enumerable => $"{statement}.Select(m => ({itemType})m)",
            CollectionType.Array => $"{statement}.Select(m => ({itemType})m).ToArray()",
            CollectionType.List => $"{statement}.Select(m => ({itemType})m).ToList()",
            CollectionType.Collection => $"new {ts.GetTypeName(toNullable)}({statement}.Select(m => ({itemType})m))",
            _ => $"({ts}){statement}"
        };

        return result;
    }


    public static bool IsShadowNullable(this ITypeSymbol type)
    {
        return type.GetAttributes().Any(m =>
                                        m.AttributeClass?.Name == TARGET_ATTRIBUTE ||
                                        m.AttributeClass?.Name == TARGET_SHORT_ATTRIBUTE);
    }

    public static (CollectionType descriminator, string itemType) GetCollectionType(
                        this ITypeSymbol type, 
                        bool toNullable = false)
    {
        if (type is IArrayTypeSymbol arr)
        {
            if (arr.ElementType.IsShadowNullable())
            {
                var t = arr.ElementType;
                if (toNullable)
                    return (CollectionType.Array, GetTypeName(t));
                return (CollectionType.Array, t.ToDisplayString());
            }
            return (CollectionType.None, type.ToDisplayString());
        }

        var parts = type.ToDisplayParts(SymbolDisplayFormat.MinimallyQualifiedFormat);
        SymbolDisplayPart[] cols = parts
                        .SkipWhile(m => m.Kind != SymbolDisplayPartKind.Punctuation && m.Symbol?.Name != "<")
                        .Skip(1)
                        .TakeWhile(m => m.Kind != SymbolDisplayPartKind.Punctuation ||
                                    m.ToString() switch { "?" => true, "," => true, _ => false })
                        .Where(m => m.Symbol is ITypeSymbol ts && ts.IsShadowNullable())
                        .ToArray();
        if (cols.Length != 1)
            return (CollectionType.None, type.ToDisplayString());
        SymbolDisplayPart col = cols.Single();

        if (type.IsIList())
            return (CollectionType.List, FormatType());
        if (type.IsICollection())
            return (CollectionType.Collection, FormatType());
        if (type.IsIEnumerable())
            return (CollectionType.Enumerable, FormatType());
        return (CollectionType.None, type.ToDisplayString());

        string FormatType()
        {
            if (toNullable && col.Symbol is ITypeSymbol t)
                return GetTypeName(t);
            return col.ToString();
        }
    }

    public static bool IsIEnumerable(this ITypeSymbol type) => type.Name == "IEnumerable";

    public static bool IsArray(this ITypeSymbol type) => type is IArrayTypeSymbol;

    public static bool IsICollection(this ITypeSymbol type)
    {
        return type.AllInterfaces.Any(m => m.Name == "ICollection");
    }

    public static bool IsIList(this ITypeSymbol type) => type.Name == "List" || type.Name == "IList";
}
