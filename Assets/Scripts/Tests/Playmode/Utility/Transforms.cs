using NUnit.Framework;
using Unity.Mathematics;

public class Transforms
{
    [Test]
    public void DistanceTest()
    {
        float3 p1 = new float3(0, 0, 0);
        float3 p2 = new float3(1, 0, 0);
        float3 p3 = new float3(0, 2, 0);
        float3 p4 = new float3(-3, 0, 3);

        Assert.That(System.Math.Round(p1.Distance(p2), 2) == 1.0d);
        Assert.That(System.Math.Round(p2.Distance(p3), 2) == 2.24d);
        Assert.That(System.Math.Round(p3.Distance(p4), 2) == 4.69d);
        Assert.That(System.Math.Round(p4.Distance(p1), 2) == 4.24d);
    }
}
