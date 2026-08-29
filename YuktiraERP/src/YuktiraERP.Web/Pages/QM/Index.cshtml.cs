using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Authorization;
using YuktiraERP.Core.Interfaces;
using YuktiraERP.Infrastructure.Data.Entities;

namespace YuktiraERP.Web.Pages.QM;

[Authorize]
public class IndexModel : PageModel
{
    private readonly IRepository<InspectionLotEntity, Guid> _lotRepo;
    private readonly IRepository<InspectionResultEntity, Guid> _resultRepo;
    private readonly IRepository<UsageDecisionEntity, Guid> _decisionRepo;
    private readonly IRepository<QualityNotificationEntity, Guid> _notifRepo;
    private readonly IRepository<InspectionPlanEntity, Guid> _planRepo;
    private readonly IRepository<CertificateOfAnalysisEntity, Guid> _coaRepo;

    public List<InspectionLotEntity> InspectionLots { get; set; } = new();
    public List<InspectionResultEntity> InspectionResults { get; set; } = new();
    public List<UsageDecisionEntity> UsageDecisions { get; set; } = new();
    public List<QualityNotificationEntity> Notifications { get; set; } = new();
    public List<InspectionPlanEntity> InspectionPlans { get; set; } = new();
    public List<CertificateOfAnalysisEntity> Certificates { get; set; } = new();

    public int PendingTestingCount { get; set; }
    public int FirstPassYield { get; set; }
    public int UDsTodayCount { get; set; }
    public int OOSAlertCount { get; set; }

    public IndexModel(
        IRepository<InspectionLotEntity, Guid> lotRepo,
        IRepository<InspectionResultEntity, Guid> resultRepo,
        IRepository<UsageDecisionEntity, Guid> decisionRepo,
        IRepository<QualityNotificationEntity, Guid> notifRepo,
        IRepository<InspectionPlanEntity, Guid> planRepo,
        IRepository<CertificateOfAnalysisEntity, Guid> coaRepo)
    {
        _lotRepo = lotRepo;
        _resultRepo = resultRepo;
        _decisionRepo = decisionRepo;
        _notifRepo = notifRepo;
        _planRepo = planRepo;
        _coaRepo = coaRepo;
    }

    public async Task OnGetAsync()
    {
        InspectionLots = await _lotRepo.GetAllAsync();
        InspectionResults = await _resultRepo.GetAllAsync();
        UsageDecisions = await _decisionRepo.GetAllAsync();
        Notifications = await _notifRepo.GetAllAsync();
        InspectionPlans = await _planRepo.GetAllAsync();
        Certificates = await _coaRepo.GetAllAsync();

        var today = DateTime.UtcNow.Date;
        PendingTestingCount = InspectionLots.Count(l => l.Status == "Created" || l.Status == "In Testing");
        var totalLotQty = InspectionLots.Sum(l => int.TryParse(l.Quantity, out var q) ? q : 0);
        var totalPassed = InspectionResults.Count(r => r.Evaluation == "Pass");
        var totalResults = InspectionResults.Count;
        FirstPassYield = totalResults > 0 ? (int)Math.Round((double)totalPassed / totalResults * 100) : 100;
        UDsTodayCount = UsageDecisions.Count(d => d.DecisionDate.Date == today);
        OOSAlertCount = InspectionResults.Count(r => r.Evaluation == "Fail");
    }
}
