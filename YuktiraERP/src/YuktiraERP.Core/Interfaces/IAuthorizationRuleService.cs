using YuktiraERP.Core.Domain.Security;

namespace YuktiraERP.Core.Interfaces;

public interface IAuthorizationRuleService
{
    Task<AuthorizationEvaluationResult> EvaluateDocumentAuthorizationAsync(
        AuthorizationEvaluationRequest request,
        Guid? tenantId = null,
        CancellationToken cancellationToken = default);

    Task<bool> RegisterRuleAsync(
        AuthorizationRule rule,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<AuthorizationRule>> GetRulesForDocumentTypeAsync(
        string documentType,
        Guid? tenantId = null,
        CancellationToken cancellationToken = default);
}
