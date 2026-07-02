namespace YuktiraERP.Core.Interfaces;

public class WorkCenterLoadDto
{
    public Guid WorkCenterId { get; set; }
    public string Code { get; set; } = "";
    public string Name { get; set; } = "";
    public string Department { get; set; } = "";
    public decimal AvailableHours { get; set; }
    public decimal RequiredHours { get; set; }
    public decimal LoadPercent { get; set; }
    public int OperationCount { get; set; }
    public string Status { get; set; } = "";
    public List<OperationLoadDetailDto> Operations { get; set; } = new();
}

public class OperationLoadDetailDto
{
    public string ProductName { get; set; } = "";
    public string ProductionOrderNumber { get; set; } = "";
    public int OperationNo { get; set; }
    public string WorkCenterCode { get; set; } = "";
    public decimal SetupTimeHrs { get; set; }
    public decimal RunTimePerUnitHrs { get; set; }
    public decimal Quantity { get; set; }
    public decimal TotalRequiredHours { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
}

public interface ICapacityPlanningService
{
    Task<List<WorkCenterLoadDto>> CalculateLoadAsync(Guid tenantId, DateTime? startDate = null, DateTime? endDate = null);
    Task<WorkCenterLoadDto?> GetWorkCenterLoadAsync(Guid workCenterId, DateTime startDate, DateTime endDate);
    Task<List<OperationLoadDetailDto>> GetOperationsForWorkCenterAsync(Guid workCenterId, DateTime startDate, DateTime endDate);
}
