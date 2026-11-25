using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using YuktiraERP.Web.Pages;

namespace YuktiraERP.Web.Pages.MES;

[Authorize]
public class IndexModel : YuktiraPageModel
{
    public MesKpiDto Kpis { get; set; } = new();
    public List<WorkOrderQueueItem> DispatchQueue { get; set; } = new();
    public List<ResourceStatusItem> Resources { get; set; } = new();
    public List<OeeMetricItem> OeeMetrics { get; set; } = new();
    public List<GenealogyItem> GenealogyNodes { get; set; } = new();
    public List<ProcessAlarmItem> Alarms { get; set; } = new();

    public void OnGet()
    {
        Kpis = new MesKpiDto
        {
            ActiveWorkOrders = 8,
            RunningMachines = 12,
            TotalMachines = 14,
            PlantOee = 86.4m,
            Availability = 92.5m,
            Performance = 95.8m,
            QualityRate = 97.5m,
            ActiveAlarms = 1,
            OpenHolds = 0
        };

        DispatchQueue = new List<WorkOrderQueueItem>
        {
            new() { WorkOrder = "WO-2026-0891", MaterialCode = "FG-001", MaterialName = "Finished Product A", Operation = "OP-10 Extrusion", Resource = "EXTRUDER-01", PlannedQty = 500, CompletedQty = 320, ScrapQty = 2, Status = "RUNNING", Priority = 1 },
            new() { WorkOrder = "WO-2026-0892", MaterialCode = "FG-001", MaterialName = "Finished Product A", Operation = "OP-20 Assembly", Resource = "ASSY-LINE-01", PlannedQty = 500, CompletedQty = 150, ScrapQty = 0, Status = "RUNNING", Priority = 2 },
            new() { WorkOrder = "WO-2026-0893", MaterialCode = "PK-001", MaterialName = "Packaging Box", Operation = "OP-30 Packaging", Resource = "PACK-STATION-01", PlannedQty = 5000, CompletedQty = 0, ScrapQty = 0, Status = "READY", Priority = 3 },
            new() { WorkOrder = "WO-2026-0894", MaterialCode = "RM-001", MaterialName = "Raw Material Pre-Mix", Operation = "OP-05 Blending", Resource = "MIXER-02", PlannedQty = 1200, CompletedQty = 1200, ScrapQty = 5, Status = "COMPLETED", Priority = 4 }
        };

        Resources = new List<ResourceStatusItem>
        {
            new() { ResourceId = "EXTRUDER-01", Name = "High-Speed Extruder #1", WorkCenter = "WC-EXTRUDE", Status = "RUNNING", StateColor = "success", CurrentOrder = "WO-2026-0891", SpeedRpm = 1450, TempC = 185.4, Utilization = 94.2m },
            new() { ResourceId = "EXTRUDER-02", Name = "Extruder #2 (Standby)", WorkCenter = "WC-EXTRUDE", Status = "IDLE", StateColor = "warning", CurrentOrder = "-", SpeedRpm = 0, TempC = 42.0, Utilization = 48.0m },
            new() { ResourceId = "ASSY-LINE-01", Name = "Automated Assembly Line A", WorkCenter = "WC-ASSEMBLY", Status = "RUNNING", StateColor = "success", CurrentOrder = "WO-2026-0892", SpeedRpm = 120, TempC = 24.1, Utilization = 96.5m },
            new() { ResourceId = "PACK-STATION-01", Name = "Cartoning & Packing Station", WorkCenter = "WC-PACK", Status = "SETUP", StateColor = "info", CurrentOrder = "WO-2026-0893", SpeedRpm = 0, TempC = 22.5, Utilization = 72.1m },
            new() { ResourceId = "MIXER-02", Name = "Ribbon Blender 500L", WorkCenter = "WC-BLEND", Status = "CLEANING", StateColor = "secondary", CurrentOrder = "-", SpeedRpm = 0, TempC = 28.0, Utilization = 85.0m }
        };

        OeeMetrics = new List<OeeMetricItem>
        {
            new() { WorkCenter = "WC-EXTRUDE", Availability = 94.1m, Performance = 96.2m, Quality = 98.4m, OverallOee = 89.1m, LossReason = "Minor stops: nozzle cleaning (14 min)" },
            new() { WorkCenter = "WC-ASSEMBLY", Availability = 96.0m, Performance = 97.1m, Quality = 99.0m, OverallOee = 92.3m, LossReason = "Component feeder reload (8 min)" },
            new() { WorkCenter = "WC-PACK", Availability = 88.5m, Performance = 92.0m, Quality = 96.5m, OverallOee = 78.6m, LossReason = "Roll changeover (25 min)" },
            new() { WorkCenter = "WC-BLEND", Availability = 91.2m, Performance = 94.0m, Quality = 98.0m, OverallOee = 84.0m, LossReason = "CIP sanitize cycle (35 min)" }
        };

        GenealogyNodes = new List<GenealogyItem>
        {
            new() { LotNumber = "LOT-2026-004", ItemCode = "FG-001", ItemName = "Finished Product A", ParentLot = "-", Level = 0, Qty = "500 EA", Timestamp = "2026-11-25 14:30 UTC", Operator = "OPR-502" },
            new() { LotNumber = "LOT-2026-001", ItemCode = "RM-001", ItemName = "Raw Material X", ParentLot = "LOT-2026-004", Level = 1, Qty = "1,000 KG", Timestamp = "2026-11-25 11:15 UTC", Operator = "OPR-401" },
            new() { LotNumber = "LOT-2026-002", ItemCode = "RM-002", ItemName = "Raw Material Y", ParentLot = "LOT-2026-004", Level = 1, Qty = "250 KG", Timestamp = "2026-11-25 11:20 UTC", Operator = "OPR-401" },
            new() { LotNumber = "LOT-2026-003", ItemCode = "PK-001", ItemName = "Packaging Box", ParentLot = "LOT-2026-004", Level = 1, Qty = "500 EA", Timestamp = "2026-11-25 14:00 UTC", Operator = "OPR-610" }
        };

        Alarms = new List<ProcessAlarmItem>
        {
            new() { Id = "ALM-902", MachineId = "EXTRUDER-01", Severity = "WARNING", Message = "Barrel Zone 3 Temperature +4.2°C above upper tolerance", Timestamp = "18:14:22", Acknowledged = true },
            new() { Id = "ALM-901", MachineId = "PACK-STATION-01", Severity = "INFO", Message = "Carton magazine level below 15%", Timestamp = "17:55:01", Acknowledged = true }
        };
    }
}

public class MesKpiDto
{
    public int ActiveWorkOrders { get; set; }
    public int RunningMachines { get; set; }
    public int TotalMachines { get; set; }
    public decimal PlantOee { get; set; }
    public decimal Availability { get; set; }
    public decimal Performance { get; set; }
    public decimal QualityRate { get; set; }
    public int ActiveAlarms { get; set; }
    public int OpenHolds { get; set; }
}

public class WorkOrderQueueItem
{
    public string WorkOrder { get; set; } = "";
    public string MaterialCode { get; set; } = "";
    public string MaterialName { get; set; } = "";
    public string Operation { get; set; } = "";
    public string Resource { get; set; } = "";
    public decimal PlannedQty { get; set; }
    public decimal CompletedQty { get; set; }
    public decimal ScrapQty { get; set; }
    public string Status { get; set; } = "";
    public int Priority { get; set; }
}

public class ResourceStatusItem
{
    public string ResourceId { get; set; } = "";
    public string Name { get; set; } = "";
    public string WorkCenter { get; set; } = "";
    public string Status { get; set; } = "";
    public string StateColor { get; set; } = "success";
    public string CurrentOrder { get; set; } = "";
    public double SpeedRpm { get; set; }
    public double TempC { get; set; }
    public decimal Utilization { get; set; }
}

public class OeeMetricItem
{
    public string WorkCenter { get; set; } = "";
    public decimal Availability { get; set; }
    public decimal Performance { get; set; }
    public decimal Quality { get; set; }
    public decimal OverallOee { get; set; }
    public string LossReason { get; set; } = "";
}

public class GenealogyItem
{
    public string LotNumber { get; set; } = "";
    public string ItemCode { get; set; } = "";
    public string ItemName { get; set; } = "";
    public string ParentLot { get; set; } = "";
    public int Level { get; set; }
    public string Qty { get; set; } = "";
    public string Timestamp { get; set; } = "";
    public string Operator { get; set; } = "";
}

public class ProcessAlarmItem
{
    public string Id { get; set; } = "";
    public string MachineId { get; set; } = "";
    public string Severity { get; set; } = "";
    public string Message { get; set; } = "";
    public string Timestamp { get; set; } = "";
    public bool Acknowledged { get; set; }
}
