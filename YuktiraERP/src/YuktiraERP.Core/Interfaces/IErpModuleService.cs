namespace YuktiraERP.Core.Interfaces;

public interface IErpModuleService
{
    string ModuleCode { get; }
    string ModuleName { get; }
    Task InitializeAsync(Guid tenantId);
    Task RegisterWorkflowsAsync(Guid tenantId);
    Task RegisterApprovalMatricesAsync(Guid tenantId);
}
