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
