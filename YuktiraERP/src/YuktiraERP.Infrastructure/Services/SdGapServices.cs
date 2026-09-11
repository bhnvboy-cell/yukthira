using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using YuktiraERP.Core.Dtos;
using YuktiraERP.Core.Enums;
using YuktiraERP.Core.Interfaces;
using YuktiraERP.Infrastructure.Data;

namespace YuktiraERP.Infrastructure.Services;

public class CreditManagementService : ICreditManagementService
{
    private readonly YuktiraDbContext _db;
    private readonly ILogger<CreditManagementService> _logger;

    public CreditManagementService(YuktiraDbContext db, ILogger<CreditManagementService> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task<CreditCheckResult_Dto> CheckCreditAsync(CreditCheckRequest request)
    {
        try
        {
            var customer = await _db.Customers.FirstOrDefaultAsync(c => c.Code == request.CustomerCode);
            if (customer == null)
                return new CreditCheckResult_Dto { Success = false, Errors = { $"Customer not found: {request.CustomerCode}" } };

            var creditLimit = customer.CreditLimit;
            var outstandingBalance = await _db.BillingDocuments
                .Where(b => b.CustomerName == customer.Name && b.Status != "Paid")
                .SumAsync(b => b.Amount);

            var availableCredit = creditLimit - outstandingBalance;
            var exposureAfterOrder = outstandingBalance + request.OrderAmount;

            CreditCheckResult result;
            if (exposureAfterOrder > creditLimit)
            {
                result = CreditCheckResult.Blocked;
                _logger.LogWarning("Credit BLOCKED for {Customer}: Exposure {Exposure} exceeds limit {Limit}",
                    request.CustomerCode, exposureAfterOrder, creditLimit);
            }
            else if (exposureAfterOrder > creditLimit * 0.9m)
            {
                result = CreditCheckResult.Warning;
            }
            else
            {
                result = CreditCheckResult.Passed;
            }

            return new CreditCheckResult_Dto
            {
                Success = true,
                Result = result,
                CreditLimit = creditLimit,
                OutstandingBalance = outstandingBalance,
                AvailableCredit = availableCredit,
                ExposureAfterOrder = exposureAfterOrder
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Credit check failed for {Customer}", request.CustomerCode);
            return new CreditCheckResult_Dto { Success = false, Errors = { ex.Message } };
        }
    }

    public async Task<List<CreditCheckResult_Dto>> GetCreditOverviewAsync(Guid tenantId, string? customerCode = null)
    {
        var query = _db.Customers.AsQueryable();
        if (!string.IsNullOrEmpty(customerCode))
            query = query.Where(c => c.Code == customerCode);

        var customers = await query.ToListAsync();
        var results = new List<CreditCheckResult_Dto>();

        foreach (var customer in customers)
        {
            var outstanding = await _db.BillingDocuments
                .Where(b => b.CustomerName == customer.Name && b.Status != "Paid")
                .SumAsync(b => b.Amount);

            results.Add(new CreditCheckResult_Dto
            {
                Success = true,
                Result = outstanding > customer.CreditLimit ? CreditCheckResult.Exceeded
                       : outstanding > customer.CreditLimit * 0.9m ? CreditCheckResult.Warning
                       : CreditCheckResult.Passed,
                CreditLimit = customer.CreditLimit,
                OutstandingBalance = outstanding,
                AvailableCredit = customer.CreditLimit - outstanding
            });
        }

        return results;
    }
}

public class SchedulingAgreementService : ISchedulingAgreementService
{
    private readonly YuktiraDbContext _db;
    private readonly ILogger<SchedulingAgreementService> _logger;

    public SchedulingAgreementService(YuktiraDbContext db, ILogger<SchedulingAgreementService> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task<SchedulingAgreementResult> CreateAgreementAsync(SchedulingAgreementRequest request)
    {
        try
        {
            var confirmedQty = request.ScheduleLines.Where(l => l.Status == ScheduleLineStatus.Confirmed).Sum(l => l.Quantity);

            _logger.LogInformation("Scheduling agreement created: {Agreement}, Customer={Customer}, Lines={Lines}",
                request.AgreementNumber, request.CustomerCode, request.ScheduleLines.Count);

            return new SchedulingAgreementResult
            {
                Success = true,
                AgreementNumber = request.AgreementNumber,
                ScheduleLineCount = request.ScheduleLines.Count,
                TotalConfirmedQuantity = confirmedQty
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create scheduling agreement");
            return new SchedulingAgreementResult { Success = false, Errors = { ex.Message } };
        }
    }

    public async Task<SchedulingAgreementResult> ConfirmScheduleLinesAsync(string agreementNumber, List<ScheduleLineDto> lines, Guid tenantId)
    {
        var confirmed = lines.Where(l => l.Status == ScheduleLineStatus.Confirmed).Sum(l => l.Quantity);
        return new SchedulingAgreementResult
        {
            Success = true,
            AgreementNumber = agreementNumber,
            ScheduleLineCount = lines.Count,
            TotalConfirmedQuantity = confirmed
        };
    }

    public Task<List<ScheduleLineDto>> GetScheduleLinesAsync(string agreementNumber, Guid tenantId)
    {
        return Task.FromResult(new List<ScheduleLineDto>());
    }
}

public class RevenueRecognitionService : IRevenueRecognitionService
{
    private readonly YuktiraDbContext _db;
    private readonly ILogger<RevenueRecognitionService> _logger;

    public RevenueRecognitionService(YuktiraDbContext db, ILogger<RevenueRecognitionService> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task<RevenueRecognitionResult> RecognizeRevenueAsync(RevenueRecognitionRequest request)
    {
        try
        {
            var totalDays = (request.ServiceEndDate - request.ServiceStartDate).Days;
            var months = Math.Max(1, (int)Math.Ceiling(totalDays / 30.0));
            var monthlyAmount = request.TotalAmount / months;

            var now = DateTime.UtcNow;
            var monthsElapsed = Math.Max(0, (now.Year - request.ServiceStartDate.Year) * 12 + now.Month - request.ServiceStartDate.Month);
            var recognizedAmount = Math.Min(request.TotalAmount, monthlyAmount * monthsElapsed);
            var deferredAmount = request.TotalAmount - recognizedAmount;

            var status = recognizedAmount >= request.TotalAmount ? RevenueRecognitionStatus.Recognized
                       : recognizedAmount > 0 ? RevenueRecognitionStatus.PartiallyRecognized
                       : RevenueRecognitionStatus.Pending;

            _logger.LogInformation("Revenue recognized: {Billing}, Amount={Amount}/{Total}, Status={Status}",
                request.BillingDocumentNumber, recognizedAmount, request.TotalAmount, status);

            return new RevenueRecognitionResult
            {
                Success = true,
                RecognitionScheduleId = Guid.NewGuid().ToString(),
                RecognizedAmount = recognizedAmount,
                DeferredAmount = deferredAmount,
                Status = status,
                PeriodsCount = months
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Revenue recognition failed for {Billing}", request.BillingDocumentNumber);
            return new RevenueRecognitionResult { Success = false, Errors = { ex.Message } };
        }
    }

    public Task<RevenueRecognitionResult> GetRecognitionScheduleAsync(string billingDocumentNumber, Guid tenantId)
    {
        return Task.FromResult(new RevenueRecognitionResult { Success = true, BillingDocumentNumber = billingDocumentNumber });
    }
}
