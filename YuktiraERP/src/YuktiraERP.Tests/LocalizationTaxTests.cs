using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Xunit;
using YuktiraERP.Infrastructure.Data;
using YuktiraERP.Infrastructure.Data.Entities;
using YuktiraERP.Infrastructure.Services;
using YuktiraERP.Core.Interfaces;

namespace YuktiraERP.Tests;

public class LocalizationTaxTests
{
    private YuktiraDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<YuktiraDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new YuktiraDbContext(options);
    }

    [Fact]
    public async Task TAX01_CalculateTax_GST()
    {
        var db = CreateDb();
        var service = new LocalizationTaxService(db);

        var result = await service.CalculateTaxAsync(new LocalizationTaxCalculationRequest
        {
            CountryCode = "IN",
            TaxType = "GST",
            BaseAmount = 100000,
            Currency = "INR",
            TransactionDate = DateTime.UtcNow
        });

        Assert.True(result.Success);
        Assert.Equal("IN", result.CountryCode);
        Assert.Equal(100000, result.BaseAmount);
        Assert.True(result.TotalTaxAmount > 0);
        Assert.True(result.TotalAmountWithTax > result.BaseAmount);
        Assert.Equal(result.BaseAmount + result.TotalTaxAmount, result.TotalAmountWithTax);
        Assert.Single(result.Taxes);

        var txn = await db.TaxTransactions.FirstOrDefaultAsync(t => t.NetAmount == 100000);
        Assert.NotNull(txn);
        Assert.Equal("Posted", txn.Status);
    }

    [Fact]
    public async Task TAX02_GetSupportedCountries_ReturnsIndia()
    {
        var db = CreateDb();
        var service = new LocalizationTaxService(db);

        db.LocalizationCountries.Add(new LocalizationCountryEntity
        {
            CountryCode = "IN", CountryName = "India", Currency = "INR",
            TaxSystem = "GST", IsSupported = true
        });
        db.LocalizationCountries.Add(new LocalizationCountryEntity
        {
            CountryCode = "US", CountryName = "United States", Currency = "USD",
            TaxSystem = "SalesTax", IsSupported = true
        });
        db.LocalizationCountries.Add(new LocalizationCountryEntity
        {
            CountryCode = "GB", CountryName = "United Kingdom", Currency = "GBP",
            TaxSystem = "VAT", IsSupported = true
        });
        await db.SaveChangesAsync();

        var result = await service.GetSupportedCountriesAsync(new SupportedCountriesRequest
        {
            ActiveOnly = true
        });

        Assert.True(result.TotalCount >= 3);
        Assert.Contains(result.Countries, c => c.CountryCode == "IN");
        Assert.Contains(result.Countries, c => c.CountryCode == "US");
        var india = result.Countries.First(c => c.CountryCode == "IN");
        Assert.Equal("India", india.CountryName);
        Assert.Equal("INR", india.Currency);
        Assert.Contains("GST", india.SupportedTaxTypes);
    }

    [Fact]
    public async Task TAX03_GenerateTaxReturn_CalculatesNet()
    {
        var db = CreateDb();
        var service = new LocalizationTaxService(db);

        var result = await service.GenerateTaxReturnAsync(new TaxReturnGenerateRequest
        {
            CountryCode = "IN",
            TaxType = "GST",
            FiscalYear = "2026",
            FiscalPeriod = 3
        });

        Assert.True(result.Success);
        Assert.NotEmpty(result.ReturnId);
        Assert.True(result.TotalTaxableAmount > 0);
        Assert.True(result.TotalTaxAmount > 0);
        Assert.True(result.TotalCredits > 0);
        Assert.Equal(5, result.LineItems.Count);
        Assert.Contains(result.LineItems, l => l.Category == "Sales");
        Assert.Contains(result.LineItems, l => l.Category == "Purchases");
        Assert.Contains(result.LineItems, l => l.Category == "Summary");
        Assert.Contains("2026", result.ReturnPeriod);

        var taxReturn = await db.TaxReturns.FirstOrDefaultAsync(t => t.Id.ToString() == result.ReturnId);
        Assert.NotNull(taxReturn);
        Assert.Equal("Draft", taxReturn.Status);
    }

    [Fact]
    public async Task TAX04_CalculateWHT_TDS()
    {
        var db = CreateDb();
        var service = new LocalizationTaxService(db);

        var result = await service.CalculateWHTAsync(new WhtCalculationRequest
        {
            CountryCode = "IN",
            GrossAmount = 100000,
            Currency = "INR",
            PaymentType = "Professional",
            IsResident = true,
            PaymentDate = DateTime.UtcNow
        });

        Assert.True(result.Success);
        Assert.Equal("IN", result.CountryCode);
        Assert.Equal(100000, result.GrossAmount);
        Assert.Equal(10m, result.WhtRate);
        Assert.Equal(10000, result.WhtAmount);
        Assert.Equal(90000, result.NetPaymentAmount);
        Assert.Equal("194J", result.SectionCode);

        var contractorResult = await service.CalculateWHTAsync(new WhtCalculationRequest
        {
            CountryCode = "IN",
            GrossAmount = 50000,
            Currency = "INR",
            PaymentType = "Contractor",
            IsResident = true,
            PaymentDate = DateTime.UtcNow
        });

        Assert.Equal(1m, contractorResult.WhtRate);
        Assert.Equal("194C", contractorResult.SectionCode);
    }
}
