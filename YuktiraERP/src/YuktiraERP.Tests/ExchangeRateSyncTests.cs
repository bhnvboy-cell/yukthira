using System.Collections.Generic;
using Xunit;
using YuktiraERP.Core.Dtos;
using YuktiraERP.Infrastructure.BackgroundServices;

namespace YuktiraERP.Tests;

public class ExchangeRateSyncTests
{
    private const string SampleEcbXml = """
        <?xml version="1.0" encoding="UTF-8"?>
        <gesmes:Envelope xmlns:gesmes="http://www.gesmes.org/xml/2002-08-01" xmlns="http://www.ecb.int/vocabulary/2002-08-01/eurofxref">
            <gesmes:subject>Reference rates</gesmes:subject>
            <gesmes:Cube>
                <gesmes:Cube time="2026-10-02">
                    <Cube currency="USD" rate="1.1000"/>
                    <Cube currency="INR" rate="96.8000"/>
                    <Cube currency="GBP" rate="0.8700"/>
                    <Cube currency="JPY" rate="172.5000"/>
                </gesmes:Cube>
            </gesmes:Cube>
        </gesmes:Envelope>
        """;

    [Fact]
    public void EcbDailyXml_EurBase_ReturnsFeedRatesWithBaseAtOne()
    {
        var rates = EcbExchangeRateProvider.ParseDailyXml(SampleEcbXml, "EUR");

        Assert.Equal(5, rates.Count); // USD, INR, GBP, JPY from the feed plus EUR
        Assert.Equal(1m, rates["EUR"]);
        Assert.Equal(1.1m, rates["USD"]);
        Assert.Equal(96.8m, rates["INR"]);
        Assert.Equal(0.87m, rates["GBP"]);
        Assert.Equal(172.5m, rates["JPY"]);
    }

    [Fact]
    public void EcbDailyXml_UsdBase_RebasesEurQuotedRates()
    {
        var rates = EcbExchangeRateProvider.ParseDailyXml(SampleEcbXml, "USD");

        Assert.Equal(1m, rates["USD"]);
        Assert.Equal(96.8m / 1.1m, rates["INR"], 6);
        Assert.Equal(1m / 1.1m, rates["EUR"], 6);
        Assert.Equal(172.5m / 1.1m, rates["JPY"], 6);
    }

    [Fact]
    public void TryResolveRate_UsesExactPairThenInversePair()
    {
        var rates = new Dictionary<(string From, string To), decimal>
        {
            [("EUR", "USD")] = 1.1m,
            [("GBP", "EUR")] = 1.2m
        };

        Assert.True(ExchangeRateMath.TryResolveRate(rates, "EUR", "USD", out var direct));
        Assert.Equal(1.1m, direct);

        Assert.True(ExchangeRateMath.TryResolveRate(rates, "USD", "EUR", out var inverse));
        Assert.Equal(1m / 1.1m, inverse, 6);

        Assert.True(ExchangeRateMath.TryResolveRate(rates, "EUR", "GBP", out var viaInverse));
        Assert.Equal(1m / 1.2m, viaInverse, 6);
    }

    [Fact]
    public void TryResolveRate_CrossesThroughCommonBase_AndFailsWithoutPath()
    {
        var rates = new Dictionary<(string From, string To), decimal>
        {
            [("EUR", "USD")] = 1.1m,
            [("EUR", "INR")] = 99m
        };

        Assert.True(ExchangeRateMath.TryResolveRate(rates, "USD", "INR", out var cross));
        Assert.Equal(90m, cross, 6);

        Assert.True(ExchangeRateMath.TryResolveRate(rates, "USD", "USD", out var same));
        Assert.Equal(1m, same);

        Assert.False(ExchangeRateMath.TryResolveRate(rates, "USD", "GBP", out _));
    }
}
