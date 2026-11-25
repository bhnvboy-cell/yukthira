using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using YuktiraERP.Core.Domain.Security;
using YuktiraERP.Core.Interfaces;
using YuktiraERP.Infrastructure.Data;

namespace YuktiraERP.Infrastructure.Services;

public class AuthorizationRuleService : IAuthorizationRuleService
{
    private readonly YuktiraDbContext _dbContext;
    private readonly ILogger<AuthorizationRuleService>? _logger;

    public AuthorizationRuleService(YuktiraDbContext dbContext, ILogger<AuthorizationRuleService>? logger = null)
    {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        _logger = logger;
    }

    public async Task<AuthorizationEvaluationResult> EvaluateDocumentAuthorizationAsync(
        AuthorizationEvaluationRequest request,
        Guid? tenantId = null,
        CancellationToken cancellationToken = default)
    {
        if (request == null) throw new ArgumentNullException(nameof(request));

        var rules = await GetRulesForDocumentTypeAsync(request.DocumentType, tenantId, cancellationToken);
        if (rules.Count == 0)
        {
            // Default policy: if no specific authorization rules configured, standard RBAC applies
            return AuthorizationEvaluationResult.Approved();
        }

        var matchingRules = rules
            .Where(r => request.Amount >= r.MinThreshold && request.Amount <= r.MaxThreshold)
            .OrderByDescending(r => r.MinThreshold)
            .ToList();

        if (matchingRules.Count == 0)
        {
            return AuthorizationEvaluationResult.Approved();
        }

        var userRoles = new HashSet<string>(request.UserRoles, StringComparer.OrdinalIgnoreCase);

        foreach (var rule in matchingRules)
        {
            // Check budget tolerance constraint
            if (rule.RequireBudgetCheck && request.IsBudgetExceeded)
            {
                _logger?.LogWarning("Budget check failed for {DocType} {DocId}. Higher approval by {RequiredRole} required.",
                    request.DocumentType, request.DocumentId, rule.RequiredRole);

                return AuthorizationEvaluationResult.EscalationRequired(
                    rule.RequiredRole,
                    $"Cost center budget exceeded. Document requires approval by '{rule.RequiredRole}'.",
                    budgetFailed: true);
            }

            // Check if current user holds the required role
            if (!string.IsNullOrWhiteSpace(rule.RequiredRole) && !userRoles.Contains(rule.RequiredRole))
            {
                _logger?.LogInformation("User {UserId} lacks required role '{RequiredRole}' for amount {Amount:C} on {DocType}.",
                    request.UserId, rule.RequiredRole, request.Amount, request.DocumentType);

                return AuthorizationEvaluationResult.EscalationRequired(
                    rule.RequiredRole,
                    $"Document amount {request.Amount:N2} exceeds limit. Requires approval by '{rule.RequiredRole}'.",
                    budgetFailed: false);
            }
        }

        return AuthorizationEvaluationResult.Approved();
    }

    public async Task<bool> RegisterRuleAsync(
        AuthorizationRule rule,
        CancellationToken cancellationToken = default)
    {
        try
        {
            await _dbContext.Set<AuthorizationRule>().AddAsync(rule, cancellationToken);
            await _dbContext.SaveChangesAsync(cancellationToken);
            _logger?.LogInformation("Registered authorization rule '{RuleId}' for {DocumentType}", rule.RuleId, rule.DocumentType);
            return true;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed to register authorization rule '{RuleId}'", rule.RuleId);
            return false;
        }
    }

    public async Task<IReadOnlyList<AuthorizationRule>> GetRulesForDocumentTypeAsync(
        string documentType,
        Guid? tenantId = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var query = _dbContext.Set<AuthorizationRule>()
                .Where(r => r.IsActive && r.DocumentType.ToLower() == documentType.ToLower());

            if (tenantId.HasValue)
            {
                query = query.Where(r => r.TenantId == tenantId.Value || r.TenantId == null);
            }

            var rules = await query.ToListAsync(cancellationToken);
            return rules.AsReadOnly();
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed to query authorization rules for {DocumentType}", documentType);
            return Array.Empty<AuthorizationRule>();
        }
    }
}
