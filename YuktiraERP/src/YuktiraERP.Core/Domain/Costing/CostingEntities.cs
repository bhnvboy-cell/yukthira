using System;
using System.Collections.Generic;
using YuktiraERP.Core.Domain.Audit;
using YuktiraERP.Core.Domain.Common;

namespace YuktiraERP.Core.Domain.Costing;

public class CostCenter : BaseAuditableEntity
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid CompanyId { get; set; }
    public string Code { get; set; } = string.Empty; // e.g. "CC-100-MFG", "CC-200-SALES", "CC-300-RND"
    public string Name { get; set; } = string.Empty;
    public string? ManagerName { get; set; }
    public Guid? ParentCostCenterId { get; set; }
    public bool IsActive { get; set; } = true;
}

public class CostCenterBudget : BaseAuditableEntity
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid CostCenterId { get; set; }
    public Guid AccountId { get; set; }
    public int FiscalYear { get; set; }
    public int FiscalPeriod { get; set; }
    public decimal BudgetAmount { get; set; }
    public decimal WarningThresholdPercent { get; set; } = 90m; // Alert at 90%
    public bool IsHardBlocked { get; set; } = true; // Reject PO/voucher if budget exceeded
}

public class BudgetVarianceRecord
{
    public Guid CostCenterId { get; set; }
    public string CostCenterCode { get; set; } = string.Empty;
    public Guid AccountId { get; set; }
    public string AccountCode { get; set; } = string.Empty;
    public decimal BudgetAmount { get; set; }
    public decimal ActualAmount { get; set; }
    public decimal CommittedAmount { get; set; } // Open PO commitments
    public decimal TotalExposure => ActualAmount + CommittedAmount;
    public decimal Variance => BudgetAmount - TotalExposure;
    public decimal UtilizationPercent => BudgetAmount == 0 ? 0 : Math.Round((TotalExposure / BudgetAmount) * 100m, 2);
    public bool IsOverBudget => TotalExposure > BudgetAmount;
}

public class StandardCostVarianceReport
{
    public Guid ItemId { get; set; }
    public string ItemCode { get; set; } = string.Empty;
    public decimal Quantity { get; set; }
    public decimal StandardUnitCost { get; set; }
    public decimal ActualUnitCost { get; set; }
    public decimal TotalStandardCost => Quantity * StandardUnitCost;
    public decimal TotalActualCost => Quantity * ActualUnitCost;
    public decimal CostVariance => TotalActualCost - TotalStandardCost; // Positive indicates unfavorable
    public bool IsFavorable => CostVariance <= 0;
}
