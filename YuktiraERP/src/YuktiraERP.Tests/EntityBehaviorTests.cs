using System;
using Xunit;
using YuktiraERP.Infrastructure.Data.Entities;

namespace YuktiraERP.Tests;

public class EntityBehaviorTests
{
    [Fact]
    public void SalesOrderLine_SetPricing_ComputesTotalAndValidates()
    {
        var line = new SalesOrderLineEntity();
        line.SetPricing(10m, 5.5m);
        Assert.Equal(55m, line.TotalPrice);

        Assert.Throws<InvalidOperationException>(() => line.SetPricing(0, 5m));
        Assert.Throws<InvalidOperationException>(() => line.SetPricing(5m, -1m));
    }

    [Fact]
    public void FixedAsset_DepreciationAndBookValue()
    {
        var asset = new FixedAssetEntity
        {
            Cost = 100000m,
            SalvageValue = 10000m,
            UsefulLifeYears = 5,
            PurchaseDate = new DateTime(2026, 1, 1)
        };
        asset.ValidateLifecycle();

        Assert.Equal(18000m, asset.AnnualDepreciation());

        var bookValue = asset.BookValue(new DateTime(2027, 1, 1));
        Assert.True(bookValue > 0 && bookValue <= 100000m);

        asset.MarkScrapped();
        Assert.Equal("Scrapped", asset.Status);
    }

    [Fact]
    public void FixedAsset_InvalidLifecycle_Throws()
    {
        var asset = new FixedAssetEntity { Cost = 500m, SalvageValue = 600m, UsefulLifeYears = 1 };
        Assert.Throws<InvalidOperationException>(() => asset.ValidateLifecycle());
    }

    [Fact]
    public void AREntry_ApplyReceipt_ClosesWhenFullyReceived()
    {
        var entry = new AREntryEntity { Amount = 1000m, ReceivedAmount = 400m, Status = "Open" };

        Assert.Equal(600m, entry.OutstandingAmount);

        entry.ApplyReceipt(600m);
        Assert.True(entry.IsFullyReceived);
        Assert.Equal("Closed", entry.Status);
    }

    [Fact]
    public void AREntry_ApplyReceipt_OverpayThrows()
    {
        var entry = new AREntryEntity { Amount = 100m };
        Assert.Throws<InvalidOperationException>(() => entry.ApplyReceipt(101m));
    }

    [Fact]
    public void PurchaseOrder_ValidAndInvalidTransitions()
    {
        var po = new PurchaseOrderEntity { Status = "Pending" };

        Assert.True(po.CanTransitionTo("Approved"));
        po.TransitionTo("Approved");
        Assert.Equal("Approved", po.Status);

        po.TransitionTo("Received");
        Assert.Equal("Received", po.Status);

        Assert.Throws<InvalidOperationException>(() => po.TransitionTo("Pending"));
    }

    [Fact]
    public void Delivery_TransitionsThroughLifecycle()
    {
        var delivery = new DeliveryEntity { Status = "Picked" };
        delivery.TransitionTo("Shipped");
        delivery.TransitionTo("Delivered");
        Assert.Equal("Delivered", delivery.Status);

        Assert.Throws<InvalidOperationException>(() => new DeliveryEntity { Status = "Shipped" }.TransitionTo("Picked"));
    }
}