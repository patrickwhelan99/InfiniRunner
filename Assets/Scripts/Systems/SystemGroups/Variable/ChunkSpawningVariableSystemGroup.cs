using Unity.Entities;

public partial class ChunkSpawningVariableSystemGroup : VariableRateSimulationSystemGroup
{
    public ChunkSpawningVariableSystemGroup()
    {
        RateManager = new RateUtils.VariableRateManager(5000);
    }
}
