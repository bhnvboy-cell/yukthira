using System.Threading.Tasks;
using Xunit;
using YuktiraERP.Infrastructure.Services;

namespace YuktiraERP.Tests;

public class EdiServiceTests
{
    private readonly EdiService _service = new();

    private static object SamplePo() => new
    {
        OrderNumber = "PO-0001",
        VendorName = "Supplier A",
        CustomerName = "Buyer B",
        Date = "2026-08-10",
        TotalAmount = 1000m,
        Lines = new[]
        {
            new { ItemCode = "RM-001", Quantity = 10m, UnitPrice = 5m },
            new { ItemCode = "RM-002", Quantity = 4m, UnitPrice = 2m }
        }
    };

    [Fact]
    public async Task ConvertToEdifact_ProducesValidOrdersInterchange()
    {
        var result = await _service.ConvertToEdifactAsync(SamplePo(), "PO");
        Assert.StartsWith("UNA:+.? ", result);
        Assert.Contains("UNB+UNOA:2+", result);
        Assert.Contains("UNH+1+ORDERS:D:96A:UN", result);
        Assert.Contains("BGM+220+PO-0001+9", result);
        Assert.Contains("LIN+1++RM-001:IN", result);
        Assert.Contains("LIN+2++RM-002:IN", result);
        Assert.Contains("CNT+2:2", result);
    }

    [Fact]
    public async Task ConvertToX12_ProducesValid850Transaction()
    {
        var result = await _service.ConvertToX12Async(SamplePo(), "PO");
        Assert.StartsWith("ISA*00*", result);
        Assert.Contains("ST*850*0001", result);
        Assert.Contains("BEG*00*SA*PO-0001", result);
        Assert.Contains("PO1*1*10*EA*5.00**IN*RM-001", result);
        Assert.Contains("CTT*2", result);
        Assert.Contains("SE*12*0001", result);
    }

    [Fact]
    public async Task ParseEdifact_ExtractsLinesAndHeader()
    {
        const string edi = "UNB+UNOA:2+SEND+RECV+260810:1000+999'"
            + "\nUNH+1+ORDERS:D:96A:UN'"
            + "\nBGM+220+PO-0001+9'"
            + "\nDTM+137:20260810:102'"
            + "\nLIN+1++RM-001:IN'"
            + "\nQTY+21:10:EA'"
            + "\nMOA+203:50.00'"
            + "\nUNT+5+1'"
            + "\nUNZ+1+999'";

        var parsed = await _service.ParseEdifactAsync(edi);
        var dict = Assert.IsAssignableFrom<System.Collections.Generic.IDictionary<string, object>>(parsed);
        Assert.Equal("ORDERS", dict["MessageType"]);
        Assert.Equal("10", dict["Quantity"]);
        Assert.Equal("20260810", dict["DocumentDate"]);
        Assert.Equal("PO-0001", dict["OrderNumber"] is null ? null : dict["OrderNumber"]);
    }

    [Fact]
    public async Task ParseX12_ExtractsLinesAndOrderNumber()
    {
        const string x12 = "ISA*00*          *00*          *ZZ*SEND*RECV*260810*1000*U*00401*001*0*P*>~"
            + "\nST*850*0001~"
            + "\nBEG*00*SA*PO-0007**20260811~"
            + "\nPO1*1*100*EA*25.00**IN*FG-001~"
            + "\nCTT*1~"
            + "\nSE*5*0001~"
            + "\nIEA*1*001~";

        var parsed = await _service.ParseX12Async(x12);
        var dict = Assert.IsAssignableFrom<System.Collections.Generic.IDictionary<string, object>>(parsed);
        Assert.Equal("850", dict["MessageType"]);
        Assert.Equal("PO-0007", dict["OrderNumber"]);
    }

    [Fact]
    public async Task UnsupportedDocumentType_Throws()
    {
        await Assert.ThrowsAsync<System.ArgumentException>(() => _service.ConvertToEdifactAsync(SamplePo(), "UNKNOWN"));
    }
}