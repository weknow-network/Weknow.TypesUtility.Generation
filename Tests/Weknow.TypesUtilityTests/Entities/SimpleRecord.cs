using Generator.Equals;

using Weknow.TypesUtility;

namespace Weknow.TypesUtilityTests;

[NullableShadow]
[Equatable]
internal partial record SimpleRecord
{
    public required int A { get; set; }

    public required string B { get; set;}

    public DateTime? C { get; set;}

    public string? D { get; set;}
}
