using System.CodeDom.Compiler;
using System.Collections.Immutable;
using System.Text;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

using static Weknow.TypesUtility.HelperExtensions;

namespace Weknow.TypesUtility;

// TODO: [bnaya 2022-10-24] Add ctor level attribue to select the From ctor
// TODO: [bnaya 2022-10-24] Add conventions (camel / Pascal)

[Generator]
public class PartialGenerator : IIncrementalGenerator
{
    private const string NEW_LINE = "\n";

    private const string MODIFIER_START = "Modifier";
    private readonly static Regex MODIFIER_CONVENSION = new Regex(@"Modifier\s*=\s*""(.*)""");

    #region Initialize

    /// <summary>
    /// Called to initialize the generator and register generation steps via callbacks
    /// on the <paramref name="context" />
    /// </summary>
    /// <param name="context">The <see cref="T:Microsoft.CodeAnalysis.IncrementalGeneratorInitializationContext" /> to register callbacks on</param>
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {

        #region var classDeclarations = ...

#pragma warning disable CS8619
        IncrementalValuesProvider<GenerationInput> classDeclarations =
                context.SyntaxProvider
                    .CreateSyntaxProvider(
                        predicate: static (s, _) => ShouldTriggerGeneration(s),
                        transform: static (ctx, _) => ToGenerationInput(ctx))
                    .Where(static m => m is not null);
#pragma warning restore CS8619

        #endregion // var classDeclarations = ...

        #region ShouldTriggerGeneration

        /// <summary>
        /// Indicate whether the node should trigger a source generation />
        /// </summary>
        static bool ShouldTriggerGeneration(SyntaxNode node)
        {
            if (!(node is TypeDeclarationSyntax t)) return false;

            bool hasAttributes = t.AttributeLists.Any(m => m.Attributes.Any(m1 =>
                    AttributePredicate(m1.Name.ToString())));

            return hasAttributes;
        };

        #endregion // ShouldTriggerGeneration

        IncrementalValueProvider<(Compilation, ImmutableArray<GenerationInput>)> compilationAndClasses
            = context.CompilationProvider.Combine(classDeclarations.Collect());

        // register a code generator for the triggers
        context.RegisterSourceOutput(compilationAndClasses, Generate);
    }

    #endregion // Initialize

    #region Generate

    /// <summary>
    /// Source generates loop.
    /// </summary>
    /// <param name="spc">The SPC.</param>
    /// <param name="source">The source.</param>
    private static void Generate(
        SourceProductionContext spc,
        (Compilation compilation,
        ImmutableArray<GenerationInput> items) source)
    {
        var (compilation, items) = source;
        foreach (GenerationInput item in items)
        {
            GeneratePartial(spc, compilation, item);
        }
    }

    #endregion // Generate

    #region GeneratePartial

    /// <summary>
    /// Generates a mapper.
    /// </summary>
    /// <param name="spc">The SPC.</param>
    /// <param name="compilation">The compilation.</param>
    /// <param name="item">The item.</param>
    internal static void GeneratePartial(
        SourceProductionContext spc,
        Compilation compilation,
        GenerationInput item,
        Func<string, bool>? predicate = null)
    {
        INamedTypeSymbol symbol = item.Symbol;

        //symbol.BaseType
        TypeDeclarationSyntax syntax = item.Syntax;
        var cls = syntax.Identifier.Text;

        var hierarchy = new List<INamedTypeSymbol> { symbol };
        var s = symbol.BaseType;
        while (s != null && s.Name != "Object")
        {
            hierarchy.Add(s);
            s = s.BaseType;
        }

        var prd = predicate ?? AttributePredicate;
        var args = syntax.AttributeLists.Where(m => m.Attributes.Any(m1 =>
                                                        prd(m1.Name.ToString())))
                                        .Single()
                                        .Attributes.Single(m => prd(m.Name.ToString())).ArgumentList?.Arguments;

        var modifier = args?.Select(m => m.ToString())
                .FirstOrDefault(m => m.StartsWith(MODIFIER_START))
                ?.Trim() ?? string.Empty;
        modifier = MODIFIER_CONVENSION.Replace(modifier, "$1");

        var suffix = args?.Select(m => m.ToString())
                .FirstOrDefault(m => m.StartsWith(SUFFIX_START))
                ?.Trim() ?? string.Empty;
        suffix = SUFFIX_CONVENSION.Replace(suffix, "$1");
        if (string.IsNullOrEmpty(suffix))
            suffix = SUFFIX_DEFAULT;

        if (string.IsNullOrEmpty(modifier))
            modifier = syntax.Modifiers.ToString();

        SyntaxKind kind = syntax.Kind();
        string typeKind = kind switch
        {
            SyntaxKind.RecordDeclaration => "record",
            SyntaxKind.RecordStructDeclaration => "record struct",
            SyntaxKind.StructDeclaration => "struct",
            SyntaxKind.ClassDeclaration => "class",
            _ => throw new Exception($"Illegal Type [{kind}]")
        };
        string? nsCandidate = symbol.ContainingNamespace.ToString();
        string ns = nsCandidate != null ? $"namespace {nsCandidate};{NEW_LINE}" : "";

        IPropertySymbol[] props = (IPropertySymbol[])(hierarchy.SelectMany(s => s.GetMembers().Select(m => m as IPropertySymbol).Where(m => m != null)).ToArray());
        IPropertySymbol[] publicProps = props.Where(m => m.DeclaredAccessibility == Accessibility.Public).ToArray();
        ImmutableArray<IParameterSymbol> parameters = symbol.Constructors
            .Where(m => !(m.Parameters.Length == 1 && m.Parameters[0].Type.Name == cls))
            .Aggregate((acc, c) =>
            {
                int cl = c.Parameters.Length;
                int accl = acc.Parameters.Length;
                if (cl > accl)
                    return c;
                return acc;
            })?.Parameters ?? ImmutableArray<IParameterSymbol>.Empty;
        Dictionary<string, object?> paramsKeys = parameters.ToDictionary(m => m.Name, m => null as object);
        IPropertySymbol[] propsExcludeParams = publicProps.Where(m => !paramsKeys.ContainsKey(m.Name)).ToArray();


        StringBuilder sbMapper = new();

        StringBuilder sb = new();
        sb.AppendLine(@$"
#pragma warning disable CS8600 // Converting null literal or possible null value to non-nullable type.
#pragma warning disable CS8604 // Possible null reference argument.

[System.CodeDom.Compiler.GeneratedCode(""Weknow.TypesUtility.Generation"", ""1.0.0"")]
{modifier} {typeKind} {cls!}{suffix}
{{
    #region Copy Ctor

    private {cls!}{suffix}({cls!} copy)
    {{
{string.Join(NEW_LINE,
            publicProps
                   .Select(m =>
                   {
                       string name = m.Name;
                       string fromCopy = $"copy?.{name}";
                       return $"\t\t{name} = {m.Type.CastExplicit(fromCopy, true)};";
                   }))}
    }}

    #endregion // Copy Ctor

    #region Operator overloads

    /// <summary>
    /// Performs an implicit conversion.
    /// </summary>
    /// <param name=""source"">The source</param>
    /// <returns>
    /// The result of the conversion.
    /// </returns>
    public static implicit operator {cls!}{suffix}({cls!} source) =>  source == null ? null : new {cls!}{suffix}(source);
    /// <summary>
    /// Performs an implicit conversion.
    /// </summary>
    /// <param name=""copy"">The copy from data</param>
    /// <returns>
    /// The result of the conversion.
    /// </returns>
    public static explicit operator {cls!}({cls!}{suffix} copy) 
    {{
        if(copy == null)
            return null;
        return new {cls!}(
            {string.Join(", ", parameters.Select(m =>
                   {
                       string name = m.Name;
                       string fromCopy = $"copy?.{name}";
                       string cast = m.Type.CastExplicit(fromCopy);
                       return cast;
                   }))})
            {{
{string.Join($",{NEW_LINE}",
            propsExcludeParams
                   .Select(m =>
                   {
                       string name = m.Name;
                       string fromCopy = $"copy?.{name}";
                       string cast = m.Type.CastExplicit(fromCopy);
                       return $"\t\t\t\t\t{name} = {cast}";
                   }))}
            }};
    }}

    #endregion // Operator overloads
{string.Join(NEW_LINE,
            publicProps
                   .Select(m =>
                   {
                       string name = m.Name;
                       string type = m.Type.GetTypeName();
                       if (!type.EndsWith("?")) 
                       {
                           type = $"{type}?";
                       }
                       return $"\tpublic {type} {name} {{ get; init; }}";
                   }))}
}}
");
        StringBuilder parents = new();
        SyntaxNode? parent = syntax.Parent;
        while (parent is ClassDeclarationSyntax pcls)
        {
            parents.Insert(0, $"{pcls.Identifier.Text}.");
            sb.Replace(NEW_LINE, $"{NEW_LINE}\t");
            sb.Insert(0, "\t");
            sb.Insert(0, @$"
partial class {pcls.Identifier.Text}
{{");
            sb.AppendLine(@"}
");
            parent = parent?.Parent;
        }

        sb.Insert(0,
            @$"{ns}
");
        spc.AddSource($"{parents}{cls}.Mapper.cs", sb.ToString());
    }

    #endregion // GeneratePartial

    #region ToGenerationInput

    /// <summary>
    /// Converts to generation-input.
    /// </summary>
    /// <param name="context">The context.</param>
    /// <returns></returns>
    private static GenerationInput ToGenerationInput(GeneratorSyntaxContext context)
    {
        var declarationSyntax = (TypeDeclarationSyntax)context.Node;

        var symbol = context.SemanticModel.GetDeclaredSymbol(declarationSyntax);
        if (symbol == null) throw new NullReferenceException($"Code generated symbol of {nameof(declarationSyntax)} is missing");
        return new GenerationInput(declarationSyntax, symbol as INamedTypeSymbol);
    }

    #endregion // ToGenerationInput

    #region AttributePredicate

    /// <summary>
    /// The predicate whether match to the target attribute
    /// </summary>
    private static bool AttributePredicate(string candidate)
    {
        int len = candidate.LastIndexOf(".");
        if (len != -1)
            candidate = candidate.Substring(len + 1);

        return candidate == TARGET_ATTRIBUTE ||
               candidate == TARGET_SHORT_ATTRIBUTE;
    }

    #endregion // AttributePredicate
}