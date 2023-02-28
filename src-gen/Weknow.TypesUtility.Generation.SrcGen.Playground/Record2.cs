namespace Weknow.TypesUtility.Generation.SrcGen.Playground
{
    [NullableShadow]
    internal record Record2(int X)
    {
        public int A { get; set; }

        public string B { get; set;}

        public DateTime? C { get; set;}
    }
}
