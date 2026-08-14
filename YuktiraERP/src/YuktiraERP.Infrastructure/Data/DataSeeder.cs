using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using YuktiraERP.Infrastructure.Data.Entities;

namespace YuktiraERP.Infrastructure.Data;
public class DataSeeder
{
    private readonly YuktiraDbContext _db;
    private readonly IConfiguration _configuration;

    public DataSeeder(YuktiraDbContext db, IConfiguration configuration)
    {
        _db = db;
        _configuration = configuration;
    }

    public async Task SeedAsync()
    {
        var connStr = _configuration.GetConnectionString("YuktiraDb");
        var isPgSql = !string.IsNullOrEmpty(connStr);

        if (isPgSql)
        {
            await ApplyPendingMigrationsAsync();
        }
        else
        {
            await _db.Database.EnsureCreatedAsync();
        }

        await SeedBetaTestersAsync();
        await SeedChartOfAccountsAsync();
        await SeedCoreUsersAsync();
        await SeedMasterDataAsync();
        await SeedSalesDataAsync();
        await SeedTaxCodesAsync();
        await SeedCurrenciesAsync();
        await SeedSystemConfigsAsync();
    }

    private async Task SeedSystemConfigsAsync()
    {
        if (await _db.Set<SystemConfigEntity>().AnyAsync()) return;

        var configs = new List<SystemConfigEntity>
        {
            new() { Key = "app.name", Value = "Yuktira ERP Suite", Description = "Application Name", Module = "Global" },
            new() { Key = "app.version", Value = "1.0.0", Description = "Application Version", Module = "Global" },
            new() { Key = "auth.max_login_attempts", Value = "5", Description = "Max login attempts before lockout", Module = "Auth" },
            new() { Key = "auth.password_min_length", Value = "8", Description = "Minimum password length", Module = "Auth" },
            new() { Key = "email.smtp_host", Value = "smtp.yuktira.com", Description = "SMTP Server Host", Module = "Email" },
            new() { Key = "email.smtp_port", Value = "587", Description = "SMTP Server Port", Module = "Email" },
            new() { Key = "features.enable_mfa", Value = "false", Description = "Enable Multi-Factor Authentication", Module = "Features" },
            new() { Key = "features.enable_audit", Value = "true", Description = "Enable Audit Logging", Module = "Features" },
        };
        await _db.Set<SystemConfigEntity>().AddRangeAsync(configs);
        await _db.SaveChangesAsync();
    }

    private async Task SeedCoreUsersAsync()
    {
        if (await _db.Set<AdminUserEntity>().AnyAsync()) return;

        var tenant = new TenantEntity { Code = "1000", Name = "Demo Company", Status = "ACTIVE", MaxUsers = 100 };
        await _db.Set<TenantEntity>().AddAsync(tenant);

        var hasher = new PasswordHasher<AdminUserEntity>();

        var users = new List<AdminUserEntity>
        {
            new() { UserId = "superadmin", UserName = "superadmin", Email = "superadmin@yuktira.com", Role = "SUPER_USER", IsActive = true, IsSuperUser = true, PasswordHash = "" },
            new() { UserId = "admin", UserName = "admin", Email = "admin@yuktira.com", Role = "ADMIN", IsActive = true, IsSuperUser = false, PasswordHash = "" },
            new() { UserId = "manager", UserName = "manager", Email = "manager@yuktira.com", Role = "POWER_USER", IsActive = true, IsSuperUser = false, PasswordHash = "" },
            new() { UserId = "user", UserName = "user", Email = "user@yuktira.com", Role = "NORMAL_USER", IsActive = true, IsSuperUser = false, PasswordHash = "" },
            new() { UserId = "readonly", UserName = "readonly", Email = "readonly@yuktira.com", Role = "READ_ONLY", IsActive = true, IsSuperUser = false, PasswordHash = "" },
        };

        foreach (var user in users)
            user.PasswordHash = hasher.HashPassword(user, "yuktira123");

        await _db.Set<AdminUserEntity>().AddRangeAsync(users);
        await _db.SaveChangesAsync();
    }

    private async Task SeedMasterDataAsync()
    {
        var tenant = await _db.Set<TenantEntity>().FirstOrDefaultAsync();
        var seedTenantId = tenant?.Id ?? Guid.NewGuid();

        if (!await _db.Set<MaterialMasterEntity>().AnyAsync())
        {
            var materials = new List<MaterialMasterEntity>
            {
                new() { Code = "FG-001", Name = "Finished Product A", Type = "FINISHED", UOM = "EA", Stock = 500, Price = 25.00m, Status = "Active" },
                new() { Code = "RM-001", Name = "Raw Material X", Type = "RAW", UOM = "KG", Stock = 1200, Price = 5.50m, Status = "Active" },
                new() { Code = "RM-002", Name = "Raw Material Y", Type = "RAW", UOM = "KG", Stock = 300, Price = 8.20m, Status = "Low Stock" },
                new() { Code = "PK-001", Name = "Packaging Box", Type = "PACKAGING", UOM = "EA", Stock = 5000, Price = 0.50m, Status = "Active" },
            };
            await _db.Set<MaterialMasterEntity>().AddRangeAsync(materials);
            await _db.SaveChangesAsync();
        }

        if (!await _db.Set<VendorEntity>().AnyAsync())
        {
            var vendors = new List<VendorEntity>
            {
                new() { Code = "VEN-001", Name = "ABC Supplies Ltd.", TaxId = "TX-12345", PaymentTerms = "Net 30", Phone = "+1-555-0201", Status = "Active" },
                new() { Code = "VEN-002", Name = "GlobalChem Industries", TaxId = "TX-12346", PaymentTerms = "Net 45", Phone = "+1-555-0202", Status = "Active" },
                new() { Code = "VEN-003", Name = "PackRight Corp.", TaxId = "TX-12347", PaymentTerms = "Net 30", Phone = "+1-555-0203", Status = "On Hold" },
            };
            await _db.Set<VendorEntity>().AddRangeAsync(vendors);
            await _db.SaveChangesAsync();
        }

        if (!await _db.Set<CustomerEntity>().AnyAsync())
        {
            var customers = new List<CustomerEntity>
            {
                new() { Code = "CUST-001", Name = "Acme Corporation", CreditLimit = 100000, PaymentTerms = "Net 30", Phone = "+1-555-0301", Status = "Active" },
                new() { Code = "CUST-002", Name = "Globex Industries", CreditLimit = 250000, PaymentTerms = "Net 45", Phone = "+1-555-0302", Status = "Active" },
                new() { Code = "CUST-003", Name = "Innotech Solutions", CreditLimit = 50000, PaymentTerms = "Net 30", Phone = "+1-555-0303", Status = "Credit Hold" },
            };
            await _db.Set<CustomerEntity>().AddRangeAsync(customers);
            await _db.SaveChangesAsync();
        }

        if (!await _db.Set<EmployeeEntity>().AnyAsync())
        {
            var employees = new List<EmployeeEntity>
            {
                new() { Code = "EMP-001", Name = "John Doe", Department = "Production", Designation = "Plant Manager", Mobile = "+1-555-0101", Status = "Active" },
                new() { Code = "EMP-002", Name = "Jane Smith", Department = "Quality", Designation = "QC Supervisor", Mobile = "+1-555-0102", Status = "Active" },
            };
            await _db.Set<EmployeeEntity>().AddRangeAsync(employees);
            await _db.SaveChangesAsync();
        }

        if (!await _db.Set<BillOfMaterialEntity>().AnyAsync())
        {
            var boms = new List<BillOfMaterialEntity>
            {
                new() { TenantId = seedTenantId, BomId = "BOM-001", ProductName = "Finished Product A", ComponentName = "Raw Material X", Quantity = 2, UOM = "KG", Status = "Active" },
                new() { TenantId = seedTenantId, BomId = "BOM-002", ProductName = "Finished Product A", ComponentName = "Raw Material Y", Quantity = 0.5m, UOM = "KG", Status = "Active" },
                new() { TenantId = seedTenantId, BomId = "BOM-003", ProductName = "Finished Product A", ComponentName = "Packaging Box", Quantity = 1, UOM = "EA", Status = "Active" },
            };
            await _db.Set<BillOfMaterialEntity>().AddRangeAsync(boms);
            await _db.SaveChangesAsync();
        }

        if (!await _db.Set<StockItemEntity>().AnyAsync())
        {
            var stock = new List<StockItemEntity>
            {
                new() { Bin = "A-01", MaterialName = "Raw Material X", Lot = "LOT-001", Quantity = 1200, UOM = "KG", Value = 6600, MinStock = 500, MaxStock = 2000 },
                new() { Bin = "A-02", MaterialName = "Raw Material Y", Lot = "LOT-002", Quantity = 300, UOM = "KG", Value = 2460, MinStock = 200, MaxStock = 800 },
                new() { Bin = "B-01", MaterialName = "Packaging Box", Lot = "LOT-003", Quantity = 5000, UOM = "EA", Value = 2500, MinStock = 1000, MaxStock = 10000 },
                new() { Bin = "C-01", MaterialName = "Finished Product A", Lot = "LOT-004", Quantity = 500, UOM = "EA", Value = 12500, MinStock = 100, MaxStock = 1000 },
            };
            await _db.Set<StockItemEntity>().AddRangeAsync(stock);
            await _db.SaveChangesAsync();
        }
    }

    private async Task SeedSalesDataAsync()
    {
        if (await _db.Set<SalesOrderEntity>().AnyAsync()) return;

        var so1 = new SalesOrderEntity { OrderNumber = "SO-001", CustomerName = "Acme Corporation", OrderDate = DateTime.UtcNow.AddDays(-30), ItemCount = 2, Amount = 15000, Status = "Confirmed" };
        var so2 = new SalesOrderEntity { OrderNumber = "SO-002", CustomerName = "Globex Industries", OrderDate = DateTime.UtcNow.AddDays(-20), ItemCount = 1, Amount = 8500, Status = "Confirmed" };
        var so3 = new SalesOrderEntity { OrderNumber = "SO-003", CustomerName = "Acme Corporation", OrderDate = DateTime.UtcNow.AddDays(-10), ItemCount = 3, Amount = 22000, Status = "Confirmed" };
        var so4 = new SalesOrderEntity { OrderNumber = "SO-004", CustomerName = "Innotech Solutions", OrderDate = DateTime.UtcNow.AddDays(-5), ItemCount = 1, Amount = 5000, Status = "Pending" };
        await _db.Set<SalesOrderEntity>().AddRangeAsync(so1, so2, so3, so4);

        var solines = new List<SalesOrderLineEntity>
        {
            new() { SalesOrderId = so1.Id, MaterialName = "Finished Product A", Quantity = 200, UOM = "EA", UnitPrice = 25.00m, TotalPrice = 5000 },
            new() { SalesOrderId = so1.Id, MaterialName = "Raw Material X", Quantity = 500, UOM = "KG", UnitPrice = 5.50m, TotalPrice = 2750 },
            new() { SalesOrderId = so2.Id, MaterialName = "Finished Product A", Quantity = 150, UOM = "EA", UnitPrice = 25.00m, TotalPrice = 3750 },
            new() { SalesOrderId = so3.Id, MaterialName = "Finished Product A", Quantity = 300, UOM = "EA", UnitPrice = 25.00m, TotalPrice = 7500 },
            new() { SalesOrderId = so3.Id, MaterialName = "Raw Material X", Quantity = 400, UOM = "KG", UnitPrice = 5.50m, TotalPrice = 2200 },
            new() { SalesOrderId = so3.Id, MaterialName = "Packaging Box", Quantity = 1000, UOM = "EA", UnitPrice = 0.50m, TotalPrice = 500 },
            new() { SalesOrderId = so4.Id, MaterialName = "Finished Product A", Quantity = 100, UOM = "EA", UnitPrice = 25.00m, TotalPrice = 2500 },
        };
        await _db.Set<SalesOrderLineEntity>().AddRangeAsync(solines);
        await _db.SaveChangesAsync();
    }

    private async Task SeedChartOfAccountsAsync()
    {
        if (await _db.Set<AccountEntity>().AnyAsync()) return;

        var accounts = new List<AccountEntity>
        {
            new() { AccountCode = "1000", AccountName = "Cash", Type = "Asset", Category = "Current" },
            new() { AccountCode = "1010", AccountName = "Bank Account", Type = "Asset", Category = "Current" },
            new() { AccountCode = "1100", AccountName = "Accounts Receivable", Type = "Asset", Category = "Current" },
            new() { AccountCode = "1200", AccountName = "Inventory", Type = "Asset", Category = "Current" },
            new() { AccountCode = "1300", AccountName = "Fixed Assets", Type = "Asset", Category = "Non-Current" },
            new() { AccountCode = "1400", AccountName = "Accumulated Depreciation", Type = "Asset", Category = "Non-Current" },
            new() { AccountCode = "2000", AccountName = "Accounts Payable", Type = "Liability", Category = "Current" },
            new() { AccountCode = "2100", AccountName = "PF Payable", Type = "Liability", Category = "Current" },
            new() { AccountCode = "2200", AccountName = "ESI Payable", Type = "Liability", Category = "Current" },
            new() { AccountCode = "2300", AccountName = "Tax Payable", Type = "Liability", Category = "Current" },
            new() { AccountCode = "3000", AccountName = "Owner's Equity", Type = "Equity", Category = "Capital" },
            new() { AccountCode = "4000", AccountName = "Sales Revenue", Type = "Income", Category = "Revenue" },
            new() { AccountCode = "4100", AccountName = "Other Income", Type = "Income", Category = "Revenue" },
            new() { AccountCode = "5000", AccountName = "Cost of Goods Sold", Type = "Expense", Category = "Operating" },
            new() { AccountCode = "5100", AccountName = "Salaries & Wages", Type = "Expense", Category = "Operating" },
            new() { AccountCode = "5200", AccountName = "Rent & Utilities", Type = "Expense", Category = "Operating" },
            new() { AccountCode = "5300", AccountName = "Depreciation", Type = "Expense", Category = "Operating" },
            new() { AccountCode = "5400", AccountName = "Miscellaneous Expenses", Type = "Expense", Category = "Operating" },
        };
        await _db.Set<AccountEntity>().AddRangeAsync(accounts);
        await _db.SaveChangesAsync();
    }

    private async Task SeedTaxCodesAsync()
    {
        var tenant = await _db.Set<TenantEntity>().FirstOrDefaultAsync();
        if (tenant == null) return;

        if (!await _db.Set<TaxCodeEntity>().AnyAsync(t => t.TenantId == tenant.Id))
        {
            var taxCodes = new List<TaxCodeEntity>
            {
                new() { TenantId = tenant.Id, Code = "GST0", Name = "GST Exempt", Rate = 0, TaxType = "GST", TaxAccountCode = "2300", IsCompound = false, IsActive = true },
                new() { TenantId = tenant.Id, Code = "GST5", Name = "GST 5%", Rate = 5, TaxType = "GST", TaxAccountCode = "2300", IsCompound = false, IsActive = true },
                new() { TenantId = tenant.Id, Code = "GST12", Name = "GST 12%", Rate = 12, TaxType = "GST", TaxAccountCode = "2300", IsCompound = false, IsActive = true },
                new() { TenantId = tenant.Id, Code = "GST18", Name = "GST 18%", Rate = 18, TaxType = "GST", TaxAccountCode = "2300", IsCompound = false, IsActive = true },
                new() { TenantId = tenant.Id, Code = "GST28", Name = "GST 28%", Rate = 28, TaxType = "GST", TaxAccountCode = "2300", IsCompound = false, IsActive = true },
                new() { TenantId = tenant.Id, Code = "VAT10", Name = "VAT 10%", Rate = 10, TaxType = "VAT", TaxAccountCode = "2300", IsCompound = false, IsActive = true },
                new() { TenantId = tenant.Id, Code = "TDS2", Name = "TDS 2%", Rate = 2, TaxType = "TDS", TaxAccountCode = "2300", IsCompound = false, IsActive = true },
            };
            await _db.Set<TaxCodeEntity>().AddRangeAsync(taxCodes);
            await _db.SaveChangesAsync();
        }
    }

    private async Task SeedCurrenciesAsync()
    {
        var tenant = await _db.Set<TenantEntity>().FirstOrDefaultAsync();
        if (tenant == null) return;

        if (!await _db.Set<CurrencyEntity>().AnyAsync(c => c.TenantId == tenant.Id))
        {
            var currencies = new List<CurrencyEntity>
            {
                new() { TenantId = tenant.Id, Code = "USD", Name = "US Dollar", Symbol = "$", IsBase = true, DecimalPlaces = 2, IsActive = true },
                new() { TenantId = tenant.Id, Code = "EUR", Name = "Euro", Symbol = "€", IsBase = false, DecimalPlaces = 2, IsActive = true },
                new() { TenantId = tenant.Id, Code = "INR", Name = "Indian Rupee", Symbol = "₹", IsBase = false, DecimalPlaces = 2, IsActive = true },
                new() { TenantId = tenant.Id, Code = "GBP", Name = "British Pound", Symbol = "£", IsBase = false, DecimalPlaces = 2, IsActive = true },
            };
            await _db.Set<CurrencyEntity>().AddRangeAsync(currencies);
            await _db.SaveChangesAsync();
        }
    }

    private async Task SeedBetaTestersAsync()
    {
        var enabled = _configuration.GetValue("Seed:BetaTesters", false);
        if (!enabled) return;

        var count = _configuration.GetValue("Seed:BetaTesterCount", 100);
        if (count <= 0) return;

        var existing = await _db.Set<AdminUserEntity>()
            .Where(u => u.UserName.StartsWith("tester"))
            .Select(u => u.UserName)
            .ToListAsync();
        var existingSet = existing.ToHashSet(StringComparer.OrdinalIgnoreCase);

        var hasher = new PasswordHasher<AdminUserEntity>();
        var testers = new List<AdminUserEntity>();

        for (var i = 1; i <= count; i++)
        {
            var userName = $"tester{i:D3}";
            if (existingSet.Contains(userName)) continue;

            testers.Add(new AdminUserEntity
            {
                UserId = userName,
                UserName = userName,
                Email = $"{userName}@yuktira.com",
                PasswordHash = hasher.HashPassword(new AdminUserEntity(), $"Test@123-{i:D3}"),
                Role = "READ_ONLY",
                IsActive = true,
                IsSuperUser = false
            });
        }

        if (testers.Count == 0) return;

        await _db.Set<AdminUserEntity>().AddRangeAsync(testers);
        await _db.SaveChangesAsync();
    }

    private async Task ApplyPendingMigrationsAsync()
    {
        var applied = new HashSet<string>();
        try
        {
            var existing = await _db.Set<MigrationEntity>().Select(m => m.Name).ToListAsync();
            applied.UnionWith(existing);
        }
        catch
        {
            await _db.Database.EnsureCreatedAsync();
        }

        var scriptsDir = Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", "database", "scripts");
        if (!Directory.Exists(scriptsDir))
        {
            var altDir = Path.Combine(Directory.GetCurrentDirectory(), "database", "scripts");
            if (Directory.Exists(altDir)) scriptsDir = altDir;
            else return;
        }

        var files = Directory.GetFiles(scriptsDir, "*.sql")
            .Select(f => new FileInfo(f))
            .OrderBy(f => f.Name)
            .ToList();

        foreach (var file in files)
        {
            var name = Path.GetFileNameWithoutExtension(file.Name);
            if (applied.Contains(name)) continue;

            var sql = await File.ReadAllTextAsync(file.FullName);
            if (string.IsNullOrWhiteSpace(sql)) continue;

            using var tx = await _db.Database.BeginTransactionAsync();
            try
            {
                await _db.Database.ExecuteSqlRawAsync(sql);
                _db.Set<MigrationEntity>().Add(new MigrationEntity { Name = name });
                await _db.SaveChangesAsync();
                await tx.CommitAsync();
            }
            catch
            {
                await tx.RollbackAsync();
                break;
            }
        }
    }
}
