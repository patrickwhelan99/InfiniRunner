using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;
public partial class VariableSystemGroupThreeSeconds : VariableRateSimulationSystemGroup
{
    public VariableSystemGroupThreeSeconds()
    {
        RateManager = new RateUtils.VariableRateManager(1000);
    }
}
