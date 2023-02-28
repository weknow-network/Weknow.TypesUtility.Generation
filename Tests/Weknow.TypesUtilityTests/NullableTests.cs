namespace Weknow.TypesUtilityTests;

public class NullableTests
{
    [Fact]
    public void NulableCastTest()
    {
        SimpleRecord r = new SimpleRecord { A = 1, B = "1", C = DateTime.Now, D = "11" };
        SimpleRecordNullable n = r;
        SimpleRecord r1 = (SimpleRecord)n;

        Assert.Equal(r, r1);
    }
    [Fact]
    public void ComplexNullableCastTest()
    {
        ComplexRecord r = new ComplexRecord 
        {
            A = 1,
            B = "1", 
            C = new ComplexRecord { A = 2, B = "2", D = Array.Empty<ComplexRecord>()},
            D = new[]
            {
                new ComplexRecord { A = 3, B = "3", D = Array.Empty<ComplexRecord>()},
                new ComplexRecord { A = 4, B = "4", D = Array.Empty<ComplexRecord>()},
            }
        };
        ComplexRecordNullable n = r;
        ComplexRecord r1 = (ComplexRecord)n;

        Assert.Equal(r.A, r1.A);
        Assert.Equal(r.B, r1.B);
        Assert.Equal(r.C, r1.C);
        Assert.True(r.D.SequenceEqual(r1.D));
    }
}