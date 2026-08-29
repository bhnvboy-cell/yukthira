using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using YuktiraERP.Infrastructure.Data.Entities;

namespace YuktiraERP.Infrastructure.Data.Configurations;

public class WarehouseTransferEntityConfiguration : IEntityTypeConfiguration<WarehouseTransferEntity>
{
    public void Configure(EntityTypeBuilder<WarehouseTransferEntity> builder)
    {
        builder.ToTable("stock_transfers", "yuktira_wm");
    }
}

public class StorageLocationEntityConfiguration : IEntityTypeConfiguration<StorageLocationEntity>
{
    public void Configure(EntityTypeBuilder<StorageLocationEntity> builder)
    {
        builder.ToTable("storage_locations", "yuktira_wm");
    }
}

public class BinEntityConfiguration : IEntityTypeConfiguration<BinEntity>
{
    public void Configure(EntityTypeBuilder<BinEntity> builder)
    {
        builder.ToTable("Bins", "yuktira_wm");
        builder.Property(e => e.MaxWeight).HasColumnType("decimal(18,4)");
        builder.Property(e => e.MaxVolume).HasColumnType("decimal(18,4)");
        builder.Property(e => e.CurrentWeight).HasColumnType("decimal(18,4)");
        builder.Property(e => e.CurrentVolume).HasColumnType("decimal(18,4)");
    }
}

public class TransferOrderEntityConfiguration : IEntityTypeConfiguration<TransferOrderEntity>
{
    public void Configure(EntityTypeBuilder<TransferOrderEntity> builder)
    {
        builder.ToTable("TransferOrders", "yuktira_wm");
        builder.Property(e => e.Quantity).HasColumnType("decimal(18,4)");
    }
}

public class WaveEntityConfiguration : IEntityTypeConfiguration<WaveEntity>
{
    public void Configure(EntityTypeBuilder<WaveEntity> builder)
    {
        builder.ToTable("Waves", "yuktira_wm");
    }
}

public class InventoryCountEntityConfiguration : IEntityTypeConfiguration<InventoryCountEntity>
{
    public void Configure(EntityTypeBuilder<InventoryCountEntity> builder)
    {
        builder.ToTable("InventoryCounts", "yuktira_wm");
    }
}
