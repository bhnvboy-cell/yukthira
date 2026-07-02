using YuktiraERP.Core.Interfaces;

namespace YuktiraERP.Infrastructure.Services;

public class PayrollService : IPayrollService
{
    // PF: 12% of basic+DA, min Rs. 1800/month, max Rs. 15,000/month (employee)
    // ESI: 0.75% employee / 3.25% employer, applicable if gross <= Rs. 21,000/month
    // Professional Tax: varies by state (using Karnataka slab)
    // TDS: estimated annual tax based on old regime slabs

    private static readonly (decimal Min, decimal Max, decimal Rate, decimal FixedAmount)[] PF_SLABS = new[]
    {
        (0m, 15000m, 0.12m, 0m),  // 12% of Basic+DA, min Rs. 1800
    };

    private static readonly (decimal Min, decimal Max, decimal Rate)[] TDS_SLABS_OLD = new[]
    {
        (0m, 250000m, 0.00m),
        (250000m, 500000m, 0.05m),
        (500000m, 1000000m, 0.20m),
        (1000000m, decimal.MaxValue, 0.30m),
    };

    public Task<PayrollCalculationResult> CalculatePayrollAsync(PayrollCalculationRequest request, string period = "")
    {
        var gross = request.BasicPay + request.HRA + request.Conveyance
                  + request.MedicalAllowance + request.SpecialAllowance
                  + request.OtherAllowance + request.Bonus;

        var basicDa = request.BasicPay;
        var pfWage = Math.Min(basicDa, 15000m);
        var pfEmployee = Math.Max(Math.Round(pfWage * 0.12m, 0), 1800m);
        var pfEmployer = pfEmployee;

        var esiApplicable = gross <= 21000m;
        var esiEmployee = esiApplicable ? Math.Round(gross * 0.0075m, 0) : 0;
        var esiEmployer = esiApplicable ? Math.Round(gross * 0.0325m, 0) : 0;

        var profTax = gross switch
        {
            <= 15000m => 0,
            <= 20000m => 150,
            <= 50000m => 300,
            _ => 600
        };

        var annualGross = gross * 12;
        var stdDeduction = 50000m;
        var taxableIncome = Math.Max(0, annualGross - pfEmployee * 12 - stdDeduction);
        var annualTax = 0m;
        foreach (var (min, max, rate) in TDS_SLABS_OLD)
        {
            if (taxableIncome > min)
            {
                var slabAmount = Math.Min(taxableIncome, max) - min;
                annualTax += slabAmount * rate;
            }
        }
        var healthCess = Math.Round(annualTax * 0.04m, 0);
        annualTax = Math.Round(annualTax + healthCess, 0);
        var tdsPerMonth = Math.Round(annualTax / 12, 0);

        var totalDeductions = pfEmployee + esiEmployee + profTax + tdsPerMonth;
        var netPay = Math.Max(0, gross - totalDeductions);
        var employerTotal = gross + pfEmployer + esiEmployer;

        var breakdown = new List<string>
        {
            $"Basic Pay: {request.BasicPay:N0}",
            $"HRA: {request.HRA:N0}",
            $"Conveyance: {request.Conveyance:N0}",
            $"Medical: {request.MedicalAllowance:N0}",
            $"Special: {request.SpecialAllowance:N0}",
            $"Other: {request.OtherAllowance:N0}",
            $"Bonus: {request.Bonus:N0}",
            $"---",
            $"Gross Pay: {gross:N0}",
            $"PF (Employee): {pfEmployee:N0}",
            $"PF (Employer): {pfEmployer:N0}",
            $"ESI (Employee): {esiEmployee:N0}",
            $"ESI (Employer): {esiEmployer:N0}",
            $"Professional Tax: {profTax:N0}",
            $"TDS: {tdsPerMonth:N0}",
            $"---",
            $"Total Deductions: {totalDeductions:N0}",
            $"Net Pay: {netPay:N0}",
            $"Employer Cost: {employerTotal:N0}"
        };

        return Task.FromResult(new PayrollCalculationResult
        {
            GrossPay = gross,
            PFEmployee = pfEmployee,
            PFEmployer = pfEmployer,
            ESIEmployee = esiEmployee,
            ESIEmployer = esiEmployer,
            ProfessionalTax = profTax,
            TDSPerMonth = tdsPerMonth,
            TotalDeductions = totalDeductions,
            NetPay = netPay,
            EmployerTotal = employerTotal,
            Breakdown = breakdown
        });
    }
}
