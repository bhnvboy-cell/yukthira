using System.Collections.Concurrent;
using YuktiraERP.Core.Interfaces;

namespace YuktiraERP.Infrastructure.Services;

public class TransactionSequenceService : ITransactionSequenceService
{
    private readonly ConcurrentDictionary<string, SequenceChainDto> _chains = new(StringComparer.OrdinalIgnoreCase);
    private readonly ConcurrentDictionary<string, SequenceInstanceDto> _instances = new();
    private readonly ITransactionCodeService _tcService;

    public TransactionSequenceService(ITransactionCodeService tcService)
    {
        _tcService = tcService;
        SeedChains();
    }

    private void SeedChains()
    {
        // Procure-to-Pay (P2P)
        Register(new SequenceChainDto
        {
            Id = "P2P", Name = "Procure-to-Pay", Module = "MM",
            Description = "End-to-end procurement cycle: PO creation through invoice verification",
            StartTCode = "ME21N", EndTCode = "MIRO",
            Steps = new()
            {
                new() { Order = 1, TCode = "ME21N", Name = "Create Purchase Order", Module = "MM",
                    Description = "Create PO with vendor, material, quantity, and pricing",
                    Prerequisites = new(), PostConditions = new() { "PO_CREATED" } },
                new() { Order = 2, TCode = "MIGO", Name = "Goods Receipt", Module = "MM",
                    MovementType = "101",
                    Description = "Post goods receipt against PO. Stock enters Quality Inspection.",
                    Prerequisites = new() { "PO_CREATED" }, PostConditions = new() { "GR_POSTED", "STOCK_IN_QI" } },
                new() { Order = 3, TCode = "QA11", Name = "Usage Decision", Module = "QM",
                    Description = "Record usage decision (Accept/Reject/Rework) on inspection lot",
                    Prerequisites = new() { "GR_POSTED" }, PostConditions = new() { "UD_POSTED" } },
                new() { Order = 4, TCode = "MIRO", Name = "Invoice Verification", Module = "MM",
                    Description = "Post vendor invoice. Three-way match: PO vs GR vs Invoice.",
                    Prerequisites = new() { "PO_CREATED", "GR_POSTED" }, PostConditions = new() { "INVOICED", "AP_OPEN" } },
            }
        });

        // Plan-to-Produce
        Register(new SequenceChainDto
        {
            Id = "P2P-PROD", Name = "Plan-to-Produce", Module = "PP",
            Description = "Full production cycle: MRP through order settlement",
            StartTCode = "MD61", EndTCode = "KO88",
            Steps = new()
            {
                new() { Order = 1, TCode = "MD61", Name = "Planned Independent Requirements", Module = "PP",
                    Description = "Create planned independent requirements (forecast/demand)",
                    Prerequisites = new(), PostConditions = new() { "PIR_CREATED" } },
                new() { Order = 2, TCode = "CO01", Name = "Create Production Order", Module = "PP",
                    Description = "Create production order with BOM and routing explosion",
                    Prerequisites = new() { "PIR_CREATED" }, PostConditions = new() { "PROD_ORDER_CREATED" } },
                new() { Order = 3, TCode = "MIGO", Name = "Goods Issue (261)", Module = "MM",
                    MovementType = "261",
                    Description = "Post goods issue of components to production order",
                    Prerequisites = new() { "PROD_ORDER_CREATED" }, PostConditions = new() { "GI_POSTED" } },
                new() { Order = 4, TCode = "CO11N", Name = "Production Order Confirmation", Module = "PP",
                    Description = "Confirm order: record yield, scrap, labor, machine time",
                    Prerequisites = new() { "GI_POSTED" }, PostConditions = new() { "ORDER_CONFIRMED" } },
                new() { Order = 5, TCode = "MIGO", Name = "Goods Receipt (101)", Module = "MM",
                    MovementType = "101",
                    Description = "Post goods receipt of finished product from production",
                    Prerequisites = new() { "ORDER_CONFIRMED" }, PostConditions = new() { "GR_POSTED", "STOCK_AVAILABLE" } },
                new() { Order = 6, TCode = "KO88", Name = "Settle Production Order", Module = "CO",
                    Description = "Settle order variances to CO receivers (cost centers, profit centers)",
                    Prerequisites = new() { "ORDER_CONFIRMED", "GR_POSTED" }, PostConditions = new() { "ORDER_SETTLED" } },
            }
        });

        // Order-to-Cash (O2C)
        Register(new SequenceChainDto
        {
            Id = "O2C", Name = "Order-to-Cash", Module = "SD",
            Description = "Full sales cycle: order creation through billing",
            StartTCode = "VA01", EndTCode = "VF01",
            Steps = new()
            {
                new() { Order = 1, TCode = "VA01", Name = "Create Sales Order", Module = "SD",
                    Description = "Create sales order with customer, material, pricing, delivery date",
                    Prerequisites = new(), PostConditions = new() { "SO_CREATED" } },
                new() { Order = 2, TCode = "VL01N", Name = "Create Outbound Delivery", Module = "SD",
                    Description = "Create delivery document from sales order",
                    Prerequisites = new() { "SO_CREATED" }, PostConditions = new() { "DELIVERY_CREATED" } },
                new() { Order = 3, TCode = "QC21", Name = "Quality Certificate (COA)", Module = "QM",
                    Description = "Generate Certificate of Analysis for delivery",
                    Prerequisites = new() { "DELIVERY_CREATED" }, PostConditions = new() { "COA_GENERATED" } },
                new() { Order = 4, TCode = "VL02N", Name = "Post Goods Issue (PGI)", Module = "SD",
                    Description = "Post goods issue. Stock reduced, cost of goods sold posted.",
                    Prerequisites = new() { "DELIVERY_CREATED" }, PostConditions = new() { "PGI_POSTED", "STOCK_REDUCED" } },
                new() { Order = 5, TCode = "VF01", Name = "Create Billing Document", Module = "SD",
                    Description = "Create invoice from delivery. AR entry posted.",
                    Prerequisites = new() { "PGI_POSTED" }, PostConditions = new() { "INVOICE_CREATED", "AR_OPEN" } },
            }
        });

        // Asset & Maintenance (PM)
        Register(new SequenceChainDto
        {
            Id = "PM-CYCLE", Name = "Asset & Maintenance Cycle", Module = "PM",
            Description = "Full PM cycle: equipment through notification, order, confirmation, settlement",
            StartTCode = "IE01", EndTCode = "KO88",
            Steps = new()
            {
                new() { Order = 1, TCode = "IE01", Name = "Equipment Master Creation", Module = "PM",
                    Description = "Create equipment master with category, functional location, work center",
                    Prerequisites = new(), PostConditions = new() { "EQUIPMENT_CREATED" } },
                new() { Order = 2, TCode = "IW21", Name = "Create Maintenance Notification", Module = "PM",
                    Description = "Create malfunction or maintenance notification for equipment",
                    Prerequisites = new() { "EQUIPMENT_CREATED" }, PostConditions = new() { "NOTIFICATION_CREATED" } },
                new() { Order = 3, TCode = "IW31", Name = "Create Maintenance Order", Module = "PM",
                    Description = "Create maintenance order with operations and spare parts",
                    Prerequisites = new() { "NOTIFICATION_CREATED" }, PostConditions = new() { "ORDER_CREATED" } },
                new() { Order = 4, TCode = "MIGO", Name = "Goods Issue Spares (261)", Module = "MM",
                    MovementType = "261",
                    Description = "Issue spare parts to maintenance order",
                    Prerequisites = new() { "ORDER_CREATED" }, PostConditions = new() { "SPARES_ISSUED" } },
                new() { Order = 5, TCode = "IW41", Name = "PM Order Confirmation", Module = "PM",
                    Description = "Confirm maintenance work: record actual hours and completion",
                    Prerequisites = new() { "ORDER_CREATED" }, PostConditions = new() { "ORDER_CONFIRMED" } },
                new() { Order = 6, TCode = "IW32", Name = "TECO - Technical Completion", Module = "PM",
                    Description = "Set order to technically complete status",
                    Prerequisites = new() { "ORDER_CONFIRMED" }, PostConditions = new() { "ORDER_TECO" } },
                new() { Order = 7, TCode = "KO88", Name = "Settle PM Order", Module = "CO",
                    Description = "Settle PM order costs to cost centers or asset",
                    Prerequisites = new() { "ORDER_TECO" }, PostConditions = new() { "ORDER_SETTLED" } },
            }
        });
    }

    private void Register(SequenceChainDto chain)
    {
        _chains[chain.Id] = chain;
    }

    public Task<List<SequenceChainDto>> GetWorkflowChainsAsync()
    {
        return Task.FromResult(_chains.Values.ToList());
    }

    public Task<SequenceChainDto?> GetChainByIdAsync(string chainId)
    {
        _chains.TryGetValue(chainId, out var chain);
        return Task.FromResult(chain);
    }

    public Task<SequenceValidationResult> ValidateStepAsync(string chainId, string tcode, Dictionary<string, object>? context = null)
    {
        if (!_chains.TryGetValue(chainId, out var chain))
        {
            return Task.FromResult(new SequenceValidationResult { IsValid = false, Message = $"Workflow chain '{chainId}' not found" });
        }

        var step = chain.Steps.FirstOrDefault(s => s.TCode.Equals(tcode, StringComparison.OrdinalIgnoreCase));
        if (step == null)
        {
            return Task.FromResult(new SequenceValidationResult { IsValid = false, Message = $"TCode '{tcode}' is not part of workflow '{chain.Name}'" });
        }

        var completedSteps = GetCompletedSteps(chainId, context);
        var missingPrereqs = step.Prerequisites.Where(p => !completedSteps.Contains(p)).ToList();

        if (missingPrereqs.Any())
        {
            return Task.FromResult(new SequenceValidationResult
            {
                IsValid = false,
                Message = $"Cannot execute '{step.Name}': missing prerequisites [{string.Join(", ", missingPrereqs)}]",
                MissingPrerequisites = missingPrereqs,
                CompletedSteps = completedSteps,
                CurrentStatus = "BLOCKED"
            });
        }

        return Task.FromResult(new SequenceValidationResult
        {
            IsValid = true,
            Message = $"Prerequisites met for '{step.Name}'",
            CompletedSteps = completedSteps,
            CurrentStatus = "READY"
        });
    }

    public async Task<SequenceExecutionResult> ExecuteStepAsync(string chainId, string tcode, Guid? userId, Dictionary<string, object>? parameters = null)
    {
        var validation = await ValidateStepAsync(chainId, tcode, parameters);
        if (!validation.IsValid)
        {
            return new SequenceExecutionResult { Success = false, Message = validation.Message };
        }

        if (!_chains.TryGetValue(chainId, out var chain))
        {
            return new SequenceExecutionResult { Success = false, Message = $"Chain '{chainId}' not found" };
        }

        var step = chain.Steps.First(s => s.TCode.Equals(tcode, StringComparison.OrdinalIgnoreCase));
        var tcResult = await _tcService.ExecuteAsync(tcode, userId, null, null, parameters);

        var completedSteps = GetCompletedSteps(chainId, parameters);
        completedSteps.AddRange(step.PostConditions);

        var nextStep = chain.Steps
            .Where(s => s.Order > step.Order)
            .OrderBy(s => s.Order)
            .FirstOrDefault(s => s.Prerequisites.All(p => completedSteps.Contains(p)));

        var allCompleted = chain.Steps.All(s =>
            s.Order <= step.Order || s.Prerequisites.All(p => completedSteps.Contains(p)));

        return new SequenceExecutionResult
        {
            Success = true,
            Message = $"Step '{step.Name}' executed successfully",
            DocumentId = tcResult.Data?.ToString(),
            NextStep = nextStep?.TCode,
            CompletedSteps = completedSteps,
            Data = new Dictionary<string, object>
            {
                ["stepOrder"] = step.Order,
                ["stepName"] = step.Name,
                ["module"] = step.Module,
                ["movementType"] = step.MovementType,
                ["isComplete"] = allCompleted,
                ["nextTCode"] = nextStep?.TCode ?? "",
                ["nextName"] = nextStep?.Name ?? ""
            }
        };
    }

    public Task<List<SequenceStepStatusDto>> GetChainProgressAsync(string chainId, string? instanceId = null)
    {
        if (!_chains.TryGetValue(chainId, out var chain))
        {
            return Task.FromResult(new List<SequenceStepStatusDto>());
        }

        var completedSteps = instanceId != null && _instances.TryGetValue(instanceId, out var inst)
            ? inst.Steps.Where(s => s.IsCompleted).Select(s => s.TCode).ToList()
            : new List<string>();

        var steps = chain.Steps.Select(s => new SequenceStepStatusDto
        {
            Order = s.Order,
            TCode = s.TCode,
            Name = s.Name,
            Status = completedSteps.Contains(s.TCode) ? "COMPLETED" :
                     s.Prerequisites.All(p => completedSteps.Contains(p)) ? "READY" : "PENDING",
            IsCurrent = false,
            IsCompleted = completedSteps.Contains(s.TCode)
        }).ToList();

        return Task.FromResult(steps);
    }

    public Task<List<SequenceInstanceDto>> GetActiveInstancesAsync(string? chainId = null)
    {
        var instances = _instances.Values
            .Where(i => i.Status == "IN_PROGRESS" && (chainId == null || i.ChainId == chainId))
            .ToList();
        return Task.FromResult(instances);
    }

    public Task<SequenceInstanceDto?> GetInstanceAsync(string instanceId)
    {
        _instances.TryGetValue(instanceId, out var inst);
        return Task.FromResult(inst);
    }

    private List<string> GetCompletedSteps(string chainId, Dictionary<string, object>? context)
    {
        var completed = new List<string>();
        if (context == null) return completed;

        var allPostConditions = _chains.TryGetValue(chainId, out var chain)
            ? chain.Steps.SelectMany(s => s.PostConditions).Distinct().ToList()
            : new List<string>();

        foreach (var cond in allPostConditions)
        {
            if (context.ContainsKey(cond) || context.ContainsKey(cond.ToLowerInvariant()))
            {
                completed.Add(cond);
            }
        }

        foreach (var kvp in context)
        {
            if (kvp.Value is bool b && b)
            {
                completed.Add(kvp.Key);
            }
        }

        return completed;
    }
}
