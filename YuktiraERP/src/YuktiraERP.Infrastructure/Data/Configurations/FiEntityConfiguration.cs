using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using YuktiraERP.Infrastructure.Data.Entities;

namespace YuktiraERP.Infrastructure.Data.Configurations;

public class JournalEntryEntityConfiguration : IEntityTypeConfiguration<JournalEntryEntity>
{
    public void Configure(EntityTypeBuilder<JournalEntryEntity> builder)
    {
        builder.ToTable("fi_documents", "yuktira_fi");
        builder.Property(e => e.Debit).HasColumnType("decimal(18,2)");
        builder.Property(e => e.Credit).HasColumnType("decimal(18,2)");
    }
}

public class APEntryEntityConfiguration : IEntityTypeConfiguration<APEntryEntity>
{
    public void Configure(EntityTypeBuilder<APEntryEntity> builder)
    {
        builder.ToTable("ap_aging", "yuktira_fi");
        builder.Property(e => e.Amount).HasColumnType("decimal(18,2)");
        builder.Property(e => e.PaidAmount).HasColumnType("decimal(18,2)");
    }
}

public class AREntryEntityConfiguration : IEntityTypeConfiguration<AREntryEntity>
{
    public void Configure(EntityTypeBuilder<AREntryEntity> builder)
    {
        builder.ToTable("ar_aging", "yuktira_fi");
        builder.Property(e => e.Amount).HasColumnType("decimal(18,2)");
        builder.Property(e => e.ReceivedAmount).HasColumnType("decimal(18,2)");
    }
}

public class FixedAssetEntityConfiguration : IEntityTypeConfiguration<FixedAssetEntity>
{
    public void Configure(EntityTypeBuilder<FixedAssetEntity> builder)
    {
        builder.ToTable("fixed_assets", "yuktira_fi");
        builder.Property(e => e.Cost).HasColumnType("decimal(18,2)");
        builder.Property(e => e.SalvageValue).HasColumnType("decimal(18,2)");
    }
}

public class CostCenterEntityConfiguration : IEntityTypeConfiguration<CostCenterEntity>
{
    public void Configure(EntityTypeBuilder<CostCenterEntity> builder)
    {
        builder.ToTable("cost_centers", "yuktira_fi");
        builder.Property(e => e.PlannedBudget).HasColumnType("decimal(18,2)");
    }
}

public class CostElementEntityConfiguration : IEntityTypeConfiguration<CostElementEntity>
{
    public void Configure(EntityTypeBuilder<CostElementEntity> builder)
    {
        builder.ToTable("cost_elements", "yuktira_fi");
    }
}

public class ProfitCenterEntityConfiguration : IEntityTypeConfiguration<ProfitCenterEntity>
{
    public void Configure(EntityTypeBuilder<ProfitCenterEntity> builder)
    {
        builder.ToTable("profit_centers", "yuktira_fi");
    }
}

public class InternalOrderEntityConfiguration : IEntityTypeConfiguration<InternalOrderEntity>
{
    public void Configure(EntityTypeBuilder<InternalOrderEntity> builder)
    {
        builder.ToTable("internal_orders", "yuktira_fi");
        builder.Property(e => e.PlannedCost).HasColumnType("decimal(18,2)");
        builder.Property(e => e.ActualCost).HasColumnType("decimal(18,2)");
    }
}

public class AccountEntityConfiguration : IEntityTypeConfiguration<AccountEntity>
{
    public void Configure(EntityTypeBuilder<AccountEntity> builder)
    {
        builder.ToTable("gl_accounts", "yuktira_fi");
        builder.Property(e => e.Balance).HasColumnType("decimal(18,2)");
    }
}

public class GeneralLedgerEntryEntityConfiguration : IEntityTypeConfiguration<GeneralLedgerEntryEntity>
{
    public void Configure(EntityTypeBuilder<GeneralLedgerEntryEntity> builder)
    {
        builder.ToTable("fi_document_lines", "yuktira_fi");
        builder.Property(e => e.Debit).HasColumnType("decimal(18,2)");
        builder.Property(e => e.Credit).HasColumnType("decimal(18,2)");
    }
}

public class TaxCodeEntityConfiguration : IEntityTypeConfiguration<TaxCodeEntity>
{
    public void Configure(EntityTypeBuilder<TaxCodeEntity> builder)
    {
        builder.ToTable("tax_codes", "yuktira_fi");
        builder.Property(e => e.Rate).HasColumnType("decimal(5,2)");
    }
}

public class TaxTransactionEntityConfiguration : IEntityTypeConfiguration<TaxTransactionEntity>
{
    public void Configure(EntityTypeBuilder<TaxTransactionEntity> builder)
    {
        builder.ToTable("tax_transactions", "yuktira_fi");
        builder.Property(e => e.Rate).HasColumnType("decimal(5,2)");
        builder.Property(e => e.NetAmount).HasColumnType("decimal(18,2)");
        builder.Property(e => e.TaxAmount).HasColumnType("decimal(18,2)");
        builder.Property(e => e.GrossAmount).HasColumnType("decimal(18,2)");
    }
}

public class CurrencyEntityConfiguration : IEntityTypeConfiguration<CurrencyEntity>
{
    public void Configure(EntityTypeBuilder<CurrencyEntity> builder)
    {
        builder.ToTable("currencies", "yuktira_fi");
    }
}

public class ExchangeRateEntityConfiguration : IEntityTypeConfiguration<ExchangeRateEntity>
{
    public void Configure(EntityTypeBuilder<ExchangeRateEntity> builder)
    {
        builder.ToTable("exchange_rates", "yuktira_fi");
        builder.Property(e => e.Rate).HasColumnType("decimal(18,6)");
    }
}

public class FiscalPeriodEntityConfiguration : IEntityTypeConfiguration<FiscalPeriodEntity>
{
    public void Configure(EntityTypeBuilder<FiscalPeriodEntity> builder)
    {
        builder.ToTable("fiscal_periods", "yuktira_fi");
    }
}

public class BankReconciliationEntityConfiguration : IEntityTypeConfiguration<BankReconciliationEntity>
{
    public void Configure(EntityTypeBuilder<BankReconciliationEntity> builder)
    {
        builder.ToTable("bank_reconciliations", "yuktira_fi");
        builder.Property(e => e.StatementBalance).HasColumnType("decimal(18,2)");
        builder.Property(e => e.LedgerBalance).HasColumnType("decimal(18,2)");
        builder.Property(e => e.Difference).HasColumnType("decimal(18,2)");
    }
}

public class PaymentEntityConfiguration : IEntityTypeConfiguration<PaymentEntity>
{
    public void Configure(EntityTypeBuilder<PaymentEntity> builder)
    {
        builder.ToTable("payments", "yuktira_fi");
        builder.Property(e => e.Amount).HasColumnType("decimal(18,2)");
    }
}

public class DepreciationScheduleEntityConfiguration : IEntityTypeConfiguration<DepreciationScheduleEntity>
{
    public void Configure(EntityTypeBuilder<DepreciationScheduleEntity> builder)
    {
        builder.ToTable("depreciation_schedules", "yuktira_fi");
        builder.Property(e => e.DepreciationAmount).HasColumnType("decimal(18,2)");
        builder.Property(e => e.AccumulatedDepreciation).HasColumnType("decimal(18,2)");
        builder.Property(e => e.BookValue).HasColumnType("decimal(18,2)");
    }
}
