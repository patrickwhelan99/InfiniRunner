using Unity.Entities;
public partial class VariableSystemGroupThreeSeconds : VariableRateSimulationSystemGroup
{
    public VariableSystemGroupThreeSeconds()
    {
        RateManager = new RateUtils.VariableRateManager(3000);
    }
}
