using Generator.Equals;

using Weknow.TypesUtility;

namespace Weknow.TypesUtilityTests;

[NullableShadow]
[Equatable]
internal partial record ComplexRecord
{
    public required int A { get; set; }

    public required string B { get; set;}

    public ComplexRecord? C { get; set;}

    public ComplexRecord[] D { get; set;}
}
