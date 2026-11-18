namespace YuktiraERP.Mes.Core.Domain.Enums;

// Function 1: Resource Allocation & Status
public enum ResourceType { MACHINE, TOOL, FIXTURE, MATERIAL_LOT, LABOR }

public enum ResourceState
{
    RUNNING,
    IDLE,
    STARVED,
    BLOCKED,
    SETUP,
    DOWN_UNPLANNED,
    DOWN_PLANNED,
    OFFLINE
}

public enum IsoLossClass { PRODUCTIVE, STANDBY, PLANNED_DOWN, UNPLANNED_DOWN, SETUP }

public enum StateChangeSource { PLC, OPERATOR, SYSTEM }

public enum AllocationStatus { RESERVED, ACTIVE, RELEASED, CONFLICT }

// Function 2: Operations & Detailed Scheduling
public enum ScheduleVersionStatus { DRAFT, PUBLISHED, FROZEN, ARCHIVED, HEURISTIC }

public enum ScheduleConflictType { CAPACITY, MATERIAL, TOOL, SKILL }

// Function 3: Dispatching Production Units
public enum WorkOrderStatus { RELEASED, IN_PROGRESS, ON_HOLD, COMPLETE, CLOSED }

public enum WoOperationStatus { QUEUED, READY, IN_SETUP, RUNNING, PAUSED, COMPLETE, REJECTED, MATERIAL_HOLD }

public enum DispatchEventType { DISPATCH, REDISPATCH, SPLIT, MERGE, REJECT }

public enum PostingStatus { PENDING, POSTED, FAILED }

// Function 4: Document Control & EBR
public enum MesDocType { WORK_INSTRUCTION, DRAWING, SOP, SETUP_SHEET, EBR_TEMPLATE, CONTROL_PLAN }

public enum DocRevisionStatus { DRAFT, IN_REVIEW, EFFECTIVE, SUPERSEDED, OBSOLETE }

public enum DocLinkType { MATERIAL, ROUTING_OP, RESOURCE, PRODUCT_FAMILY }

public enum EbrStatus { OPEN, UNDER_REVIEW, APPROVED, REJECTED }

public enum ESignatureMeaning { AUTHORED, REVIEWED, APPROVED }

// Function 5: Data Collection & Acquisition
public enum TagSemanticRole { STATE, COUNT_GOOD, COUNT_BAD, SETPOINT, TEMPERATURE, ENERGY, PRESSURE, VIBRATION }

public enum TagQuality { GOOD, UNCERTAIN, BAD }

public enum ScanResult { ACCEPTED, REJECTED, DUPLICATE }

// Function 6: Labor Management
public enum LaborClockStatus { OPEN, CLOSED, ADJUSTED }

public enum LaborActivityType { DIRECT, SETUP, REWORK, INDIRECT, TRAINING, WAITING }

public enum LaborExceptionType { NO_CLOCK_OUT, OVERLAP, OVERTIME, UNQUALIFIED }

// Function 7: Quality Management & SPC
public enum InspectionTriggerType { FIRST_OFF, PERIODIC, EVERY_N_UNITS, CONTINUOUS, END_OF_BATCH }

public enum SpcChartType { XBAR_R, IMR, P, NP, C, U }

public enum NcDisposition { USE_AS_IS, REWORK, SCRAP, RETURN }

public enum HoldTargetType { LOT, SERIAL, WORK_ORDER, RESOURCE }

public enum HoldStatus { ACTIVE, RELEASED }

// Function 8: Process Management
public enum RecipeStatus { DRAFT, APPROVED, EFFECTIVE, RETIRED }

public enum ParamControlMode { MONITOR, ALARM, INTERLOCK }

public enum AlarmState { ACTIVE, ACKED, RETURNED, CLOSED }

// Function 9: Maintenance Management
public enum MeterType { HOURS, CYCLES, UNITS }

public enum PmTriggerType { CALENDAR, METER, CONDITION }

public enum AndonCallType { MAINTENANCE, QUALITY, MATERIAL, SUPERVISOR }

// Function 10: Product Tracking & Genealogy
public enum MesLotStatus { UNRESTRICTED, QI, BLOCKED, CONSUMED, SHIPPED }

public enum LotOrigin { VENDOR, PRODUCED }

public enum GenealogyEdgeType { CONSUMED_IN, PRODUCED_FROM, SPLIT, MERGE, REWORK, PACKED_IN }

// Function 11: Performance Analysis
public enum KpiScope { RESOURCE, LINE, PLANT, PRODUCT }

public enum OeeIntervalType { MINUTE, SHIFT, DAY }

// Cross-Cutting
public enum NegativeStockPolicy { BLOCK, ALLOW_WITH_FLAG, AUTO_TRANSFER }

public enum OutboxStatus { PENDING, PUBLISHED, FAILED }
