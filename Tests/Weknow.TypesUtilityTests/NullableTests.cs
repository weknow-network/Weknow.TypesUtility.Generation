namespace Weknow.TypesUtilityTests;

public class NullableTests
{
    [Fact]
    public void NulableCastTest()
    {
        SimpleRecord r = new SimpleRecord { A = 1, B = "1", C = DateTime.Now, D = "11" };
        SimpleRecordNullable n = r;
        SimpleRecord r1 = n;
    }
}