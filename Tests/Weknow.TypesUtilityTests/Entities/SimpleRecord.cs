using Weknow.TypesUtility;

namespace Weknow.TypesUtilityTests;

[NullableShadow]
internal record SimpleRecord
{
    public required int A { get; set; }

    public required string B { get; set;}

    public DateTime? C { get; set;}

    public string? D { get; set;}
}
