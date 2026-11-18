using YuktiraERP.Mes.Core.Domain.Enums;

namespace YuktiraERP.Mes.Core.Services;

public static class ResourceStateEngine
{
    public static IsoLossClass MapToIsoLossClass(ResourceState state) => state switch
    {
        ResourceState.RUNNING => IsoLossClass.PRODUCTIVE,
        ResourceState.IDLE or ResourceState.STARVED or ResourceState.BLOCKED => IsoLossClass.STANDBY,
        ResourceState.SETUP => IsoLossClass.SETUP,
        ResourceState.DOWN_PLANNED => IsoLossClass.PLANNED_DOWN,
        ResourceState.DOWN_UNPLANNED or ResourceState.OFFLINE => IsoLossClass.UNPLANNED_DOWN,
        _ => IsoLossClass.STANDBY
    };

    public static (bool PreAlert, bool InterlockTriggered) EvaluateToolLife(long cyclesUsed, long lifeLimit)
    {
        if (lifeLimit <= 0) return (false, false);
        var ratio = (double)cyclesUsed / lifeLimit;
        return (ratio >= 0.90, ratio >= 1.00);
    }
}
