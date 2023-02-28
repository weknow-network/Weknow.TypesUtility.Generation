namespace Weknow.TypesUtility.Generation.SrcGen.Playground
{
    [NullableShadow]
    internal record DeepRecord 
    {
        public required int A { get; set; }
        public string? B { get; set; }
        public string[] StrArr { get; set; }
        public required DeepRecord Parent { get; set; }
        public required DeepRecord[] Array { get; set; }
        public required Queue<DeepRecord> Queue{ get; set; }
        public required IList<DeepRecord> List{ get; set; }
        public List<DeepRecord>? OptionalList{ get; set; }
        public required IEnumerable<DeepRecord> Items{ get; set; }
    }
}
