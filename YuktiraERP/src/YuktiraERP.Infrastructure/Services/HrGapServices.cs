using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using YuktiraERP.Core.Dtos;
using YuktiraERP.Core.Enums;
using YuktiraERP.Core.Interfaces;
using YuktiraERP.Infrastructure.Data;

namespace YuktiraERP.Infrastructure.Services;

public class BenefitsService : IBenefitsService
{
    private readonly YuktiraDbContext _db;
    private readonly ILogger<BenefitsService> _logger;

    private static readonly Dictionary<BenefitType, decimal> PremiumRates = new()
    {
        [BenefitType.HealthInsurance] = 500m,
        [BenefitType.DentalInsurance] = 150m,
        [BenefitType.VisionInsurance] = 75m,
        [BenefitType.LifeInsurance] = 200m,
        [BenefitType.RetirementPlan] = 0m,
        [BenefitType.PaidTimeOff] = 0m,
        [BenefitType.StockOption] = 0m,
    };

    public BenefitsService(YuktiraDbContext db, ILogger<BenefitsService> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task<BenefitsEnrollmentResult> EnrollBenefitAsync(BenefitsEnrollmentRequest request)
    {
        try
        {
            var employee = await _db.Employees.FirstOrDefaultAsync(e => e.Code == request.EmployeeCode);
            if (employee == null)
                return new BenefitsEnrollmentResult { Success = false, Errors = { $"Employee not found: {request.EmployeeCode}" } };

            var basePremium = PremiumRates.GetValueOrDefault(request.BenefitType, 200m);
            var totalPremium = basePremium;
            var employeeShare = request.EmployeeContribution > 0 ? request.EmployeeContribution : totalPremium * 0.3m;
            var employerShare = totalPremium - employeeShare;

            _logger.LogInformation("Benefits enrolled: Employee={Employee}, Benefit={Benefit}, Premium={Premium}",
                request.EmployeeCode, request.BenefitType, totalPremium);

            return new BenefitsEnrollmentResult
            {
                Success = true,
                EnrollmentId = Guid.NewGuid().ToString(),
                TotalPremium = totalPremium,
                EmployeeShare = employeeShare,
                EmployerShare = employerShare,
                CoverageStartDate = request.EffectiveDate
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Benefits enrollment failed for {Employee}", request.EmployeeCode);
            return new BenefitsEnrollmentResult { Success = false, Errors = { ex.Message } };
        }
    }

    public Task<List<BenefitsEnrollmentResult>> GetEmployeeBenefitsAsync(string employeeCode, Guid tenantId)
    {
        return Task.FromResult(new List<BenefitsEnrollmentResult>());
    }
}

public class SuccessionPlanningService : ISuccessionPlanningService
{
    private readonly YuktiraDbContext _db;
    private readonly ILogger<SuccessionPlanningService> _logger;

    public SuccessionPlanningService(YuktiraDbContext db, ILogger<SuccessionPlanningService> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task<SuccessionPlanResult> CreateSuccessionPlanAsync(SuccessionPlanRequest request)
    {
        try
        {
            if (!request.Candidates.Any())
                return new SuccessionPlanResult { Success = false, Errors = { "No candidates provided" } };

            var bestReadiness = request.Candidates.Min(c => c.Readiness);

            _logger.LogInformation("Succession plan created: Position={Position}, Candidates={Count}, Readiness={Readiness}",
                request.PositionCode, request.Candidates.Count, bestReadiness);

            return new SuccessionPlanResult
            {
                Success = true,
                PlanId = Guid.NewGuid().ToString(),
                PositionCode = request.PositionCode,
                CandidateCount = request.Candidates.Count,
                OverallReadiness = bestReadiness
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Succession planning failed");
            return new SuccessionPlanResult { Success = false, Errors = { ex.Message } };
        }
    }

    public Task<List<SuccessionPlanResult>> GetSuccessionPlansAsync(string positionCode, Guid tenantId)
    {
        return Task.FromResult(new List<SuccessionPlanResult>());
    }
}
