using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using YuktiraERP.Core.Domain.Security;
using YuktiraERP.Core.Interfaces;
using YuktiraERP.Infrastructure.Data;

namespace YuktiraERP.Infrastructure.Services;

public class SodConflictEngine : ISodConflictEngine
{
    private static readonly List<SodRuleEntity> PredefinedRules = new()
    {
        new SodRuleEntity
        {
            RuleCode = "SOD-001",
            RuleName = "Vendor Maintenance vs AP Invoice Posting",
            FirstRoleOrPermission = "VendorMaster_Maintain",
            SecondRoleOrPermission = "APInvoice_Post",
            Severity = SodSeverity.Strict_Block,
            Description = "Prevents creation of fictitious vendors and processing fraudulent invoices by the same user."
        },
        new SodRuleEntity
        {
            RuleCode = "SOD-002",
            RuleName = "Purchase Order Creation vs Purchase Order Approval",
            FirstRoleOrPermission = "PO_Create",
            SecondRoleOrPermission = "PO_Approve",
            Severity = SodSeverity.Strict_Block,
            Description = "Enforces Maker-Checker principle: a buyer cannot approve their own purchase order."
        },
        new SodRuleEntity
        {
            RuleCode = "SOD-003",
            RuleName = "Goods Receipt Posting vs AP Invoice Posting",
            FirstRoleOrPermission = "GoodsReceipt_Post",
            SecondRoleOrPermission = "APInvoice_Post",
            Severity = SodSeverity.Strict_Block,
            Description = "Prevents receipt of fictitious inventory followed by invoice authorization."
        },
        new SodRuleEntity
        {
            RuleCode = "SOD-004",
            RuleName = "Bank Reconciliation vs Disbursement Creation",
            FirstRoleOrPermission = "BankReconciliation_Perform",
            SecondRoleOrPermission = "Disbursement_Create",
            Severity = SodSeverity.Strict_Block,
            Description = "Prevents concealment of unauthorized bank disbursements."
        },
        new SodRuleEntity
        {
            RuleCode = "SOD-005",
            RuleName = "Credit Limit Override vs Sales Order Creation",
            FirstRoleOrPermission = "CreditLimit_Override",
            SecondRoleOrPermission = "SalesOrder_Create",
            Severity = SodSeverity.Alert_Only,
            Description = "Detects bypass of customer credit policy when creating sales orders."
        },
        new SodRuleEntity
        {
            RuleCode = "SOD-006",
            RuleName = "Inventory Count vs Inventory Adjustment Posting",
            FirstRoleOrPermission = "InventoryCount_Conduct",
            SecondRoleOrPermission = "InventoryAdjustment_Post",
            Severity = SodSeverity.Strict_Block,
            Description = "Prevents concealment of physical stock theft via ledger adjustment."
        }
    };

    private readonly YuktiraDbContext _dbContext;
    private readonly IRbacService _rbacService;
    private readonly ILogger<SodConflictEngine>? _logger;

    public SodConflictEngine(
        YuktiraDbContext dbContext,
        IRbacService rbacService,
        ILogger<SodConflictEngine>? logger = null)
    {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        _rbacService = rbacService ?? throw new ArgumentNullException(nameof(rbacService));
        _logger = logger;
    }

    public IReadOnlyList<SodRuleEntity> GetPredefinedRules() => PredefinedRules.AsReadOnly();

    public async Task<SodConflictResult> CheckRoleAssignmentConflictsAsync(
        Guid userId,
        string proposedRoleOrPermission,
        Guid? tenantId = null,
        CancellationToken cancellationToken = default)
    {
        var result = new SodConflictResult();

        // 1. Fetch user's existing roles and permissions
        var existingRoles = await _rbacService.GetUserRolesAsync(userId, tenantId, cancellationToken);
        var existingPermissions = await _rbacService.GetUserPermissionsAsync(userId, tenantId, cancellationToken);

        var currentCapabilities = new HashSet<string>(existingRoles.Concat(existingPermissions), StringComparer.OrdinalIgnoreCase);

        // 2. Fetch active rules (predefined + tenant custom rules from database)
        var allRules = new List<SodRuleEntity>(PredefinedRules);

        try
        {
            var dbRules = await _dbContext.Set<SodRuleEntity>()
                .Where(r => r.IsActive && (tenantId == null || r.TenantId == tenantId))
                .ToListAsync(cancellationToken);
            allRules.AddRange(dbRules);
        }
        catch
        {
            // Fallback to in-memory predefined rules if table is still migrating
        }

        // 3. Evaluate each rule against proposed assignment
        foreach (var rule in allRules.Where(r => r.IsActive))
        {
            bool isFirstMatch = rule.FirstRoleOrPermission.Equals(proposedRoleOrPermission, StringComparison.OrdinalIgnoreCase);
            bool isSecondMatch = rule.SecondRoleOrPermission.Equals(proposedRoleOrPermission, StringComparison.OrdinalIgnoreCase);

            if (isFirstMatch && currentCapabilities.Contains(rule.SecondRoleOrPermission))
            {
                result.Violations.Add(new SodViolation
                {
                    RuleCode = rule.RuleCode,
                    FirstConflict = proposedRoleOrPermission,
                    SecondConflict = rule.SecondRoleOrPermission,
                    Severity = rule.Severity,
                    Message = $"Assigning '{proposedRoleOrPermission}' conflicts with existing capability '{rule.SecondRoleOrPermission}': {rule.Description}"
                });
            }
            else if (isSecondMatch && currentCapabilities.Contains(rule.FirstRoleOrPermission))
            {
                result.Violations.Add(new SodViolation
                {
                    RuleCode = rule.RuleCode,
                    FirstConflict = rule.FirstRoleOrPermission,
                    SecondConflict = proposedRoleOrPermission,
                    Severity = rule.Severity,
                    Message = $"Assigning '{proposedRoleOrPermission}' conflicts with existing capability '{rule.FirstRoleOrPermission}': {rule.Description}"
                });
            }
        }

        if (result.HasConflict)
        {
            _logger?.LogWarning("SoD Conflict detected for user {UserId} assigning {Proposed}: {ViolationsCount} violations.",
                userId, proposedRoleOrPermission, result.Violations.Count);
        }

        return result;
    }

    public Task<SodConflictResult> CheckActionExecutionConflictsAsync(
        Guid userId,
        string documentType,
        string proposedAction,
        Guid? documentCreatorUserId,
        Guid? tenantId = null,
        CancellationToken cancellationToken = default)
    {
        var result = new SodConflictResult();

        // Strict Maker-Checker invariant:
        // A user cannot approve, post, or release a document they created themselves
        var sensitiveActions = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "Approve",
            "Release",
            "Post",
            "Authorize"
        };

        if (documentCreatorUserId.HasValue &&
            documentCreatorUserId.Value == userId &&
            sensitiveActions.Contains(proposedAction))
        {
            result.Violations.Add(new SodViolation
            {
                RuleCode = "SOD-MAKER-CHECKER",
                FirstConflict = "Document_Create",
                SecondConflict = $"Document_{proposedAction}",
                Severity = SodSeverity.Strict_Block,
                Message = $"Maker-Checker Invariant Violated: User '{userId}' created this '{documentType}' and cannot self-'{proposedAction}' it."
            });

            _logger?.LogWarning("Maker-Checker SoD violation: User {UserId} attempted to {Action} their own {DocumentType} document.",
                userId, proposedAction, documentType);
        }

        return Task.FromResult(result);
    }
}
