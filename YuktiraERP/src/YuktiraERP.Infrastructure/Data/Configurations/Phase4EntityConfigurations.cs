using Microsoft.EntityFrameworkCore;
using YuktiraERP.Core.Domain.Costing;
using YuktiraERP.Core.Domain.Finance;
using YuktiraERP.Core.Domain.Planning;

namespace YuktiraERP.Infrastructure.Data.Configurations;

public static class Phase4EntityConfigurations
{
    public static void ApplyPhase4Configurations(this ModelBuilder modelBuilder)
    {
        // 1. Schema: finance
        modelBuilder.Entity<ChartOfAccount>(b =>
        {
            b.ToTable("chart_of_accounts", "finance");
            b.HasKey(e => e.Id);
            b.Property(e => e.AccountCode).HasMaxLength(32).IsRequired();
            b.Property(e => e.AccountName).HasMaxLength(256).IsRequired();
            b.HasIndex(e => new { e.CompanyId, e.AccountCode }).IsUnique();
        });

        modelBuilder.Entity<JournalEntry>(b =>
        {
            b.ToTable("journal_entries", "finance");
            b.HasKey(e => e.Id);
            b.Property(e => e.EntryNumber).HasMaxLength(64).IsRequired();
            b.Property(e => e.CurrencyCode).HasMaxLength(3).IsRequired();
            b.Property(e => e.ExchangeRate).HasPrecision(18, 6);
            b.HasIndex(e => new { e.CompanyId, e.EntryNumber }).IsUnique();
            b.HasIndex(e => new { e.FiscalYear, e.FiscalPeriod });
        });

        modelBuilder.Entity<JournalEntryLine>(b =>
        {
            b.ToTable("journal_entry_lines", "finance");
            b.HasKey(e => e.Id);
            b.Property(e => e.DebitAmount).HasPrecision(18, 4);
            b.Property(e => e.CreditAmount).HasPrecision(18, 4);
            b.Property(e => e.FunctionalDebitAmount).HasPrecision(18, 4);
            b.Property(e => e.FunctionalCreditAmount).HasPrecision(18, 4);
            b.HasIndex(e => new { e.JournalEntryId, e.LineNumber });
        });

        modelBuilder.Entity<FiscalCalendarPeriod>(b =>
        {
            b.ToTable("fiscal_calendar_periods", "finance");
            b.HasKey(e => e.Id);
            b.HasIndex(e => new { e.CompanyId, e.FiscalYear, e.PeriodNumber }).IsUnique();
        });

        modelBuilder.Entity<PeriodCloseChecklistTask>(b =>
        {
            b.ToTable("period_close_tasks", "finance");
            b.HasKey(e => e.Id);
            b.Property(e => e.StepCode).HasMaxLength(64).IsRequired();
            b.HasIndex(e => new { e.FiscalPeriodId, e.StepSequence });
        });

        // 2. Schema: costing
        modelBuilder.Entity<CostCenter>(b =>
        {
            b.ToTable("cost_centers", "costing");
            b.HasKey(e => e.Id);
            b.Property(e => e.Code).HasMaxLength(64).IsRequired();
            b.HasIndex(e => new { e.CompanyId, e.Code }).IsUnique();
        });

        modelBuilder.Entity<CostCenterBudget>(b =>
        {
            b.ToTable("budgets", "costing");
            b.HasKey(e => e.Id);
            b.Property(e => e.BudgetAmount).HasPrecision(18, 4);
            b.HasIndex(e => new { e.CostCenterId, e.AccountId, e.FiscalYear, e.FiscalPeriod }).IsUnique();
        });

        // 3. Schema: planning
        modelBuilder.Entity<WorkCenterMaster>(b =>
        {
            b.ToTable("work_centers", "planning");
            b.HasKey(e => e.Id);
            b.Property(e => e.Code).HasMaxLength(64).IsRequired();
            b.Property(e => e.StandardHourlyRate).HasPrecision(18, 4);
            b.HasIndex(e => new { e.CompanyId, e.Code }).IsUnique();
        });

        modelBuilder.Entity<BillOfMaterialHeader>(b =>
        {
            b.ToTable("boms", "planning");
            b.HasKey(e => e.Id);
            b.Property(e => e.BomCode).HasMaxLength(64).IsRequired();
            b.Property(e => e.BaseQuantity).HasPrecision(18, 4);
            b.HasIndex(e => new { e.FinishedGoodItemId, e.Version });
        });

        modelBuilder.Entity<BillOfMaterialComponent>(b =>
        {
            b.ToTable("bom_components", "planning");
            b.HasKey(e => e.Id);
            b.Property(e => e.RequiredQuantity).HasPrecision(18, 4);
            b.Property(e => e.ScrapPercent).HasPrecision(18, 2);
            b.HasIndex(e => new { e.BomHeaderId, e.LineNumber });
        });

        modelBuilder.Entity<ManufacturingRouting>(b =>
        {
            b.ToTable("routings", "planning");
            b.HasKey(e => e.Id);
            b.Property(e => e.RoutingCode).HasMaxLength(64).IsRequired();
            b.HasIndex(e => new { e.ItemId, e.Version });
        });

        modelBuilder.Entity<RoutingOperationStep>(b =>
        {
            b.ToTable("routing_operations", "planning");
            b.HasKey(e => e.Id);
            b.Property(e => e.SetupTimeMinutes).HasPrecision(18, 2);
            b.Property(e => e.RunTimeMinutesPerUnit).HasPrecision(18, 2);
            b.HasIndex(e => new { e.RoutingId, e.OperationSequence });
        });

        modelBuilder.Entity<PlannedOrder>(b =>
        {
            b.ToTable("planned_orders", "planning");
            b.HasKey(e => e.Id);
            b.Property(e => e.OrderNumber).HasMaxLength(64).IsRequired();
            b.Property(e => e.Quantity).HasPrecision(18, 4);
            b.HasIndex(e => e.OrderNumber).IsUnique();
        });
    }
}
