using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Weknow.TypesUtility;

namespace Weknow.TypesUtility.Generation.SrcGen.Playground
{
    [NullableShadow(Modifier = "internal partial", Suffix = "X")]
    public class Class2
    {
        public int A { get; set; }

        public string B { get; set;}

        public DateTime? C { get; set;}
    }
}
