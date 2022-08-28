using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;

public partial class ChunkSpawningVariableSystemGroup : VariableRateSimulationSystemGroup
{
    public ChunkSpawningVariableSystemGroup()
    {
        RateManager = new RateUtils.VariableRateManager(5000);
    }
}
