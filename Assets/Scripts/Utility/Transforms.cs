using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using Unity.Burst;

public static class Transforms 
{
    [BurstCompile]
    public static float Distance(this float3 LHS, float3 RHS)
    {
        return math.sqrt( 
                            math.pow(LHS[0] - RHS[0], 2) + 
                            math.pow(LHS[1] - RHS[1], 2) + 
                            math.pow(LHS[2] - RHS[2], 2)
                        );
    }
}
