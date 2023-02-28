namespace Weknow.TypesUtility.Generation.SrcGen.Playground
{
    [NullableShadow]
    internal record Record1
    {
        public required int A { get; set; }

        public required string B { get; set;}

        public DateTime? C { get; set;}

        public string? D { get; set;}
    }
}
