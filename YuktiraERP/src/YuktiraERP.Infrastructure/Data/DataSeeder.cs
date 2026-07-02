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

        var materials = new List<MaterialMasterEntity>
        {
            new() { Code = "FG-001", Name = "Finished Product A", Type = "FINISHED", UOM = "EA", Stock = 500, Price = 25.00m, Status = "Active" },
            new() { Code = "RM-001", Name = "Raw Material X", Type = "RAW", UOM = "KG", Stock = 1200, Price = 5.50m, Status = "Active" },
            new() { Code = "RM-002", Name = "Raw Material Y", Type = "RAW", UOM = "KG", Stock = 300, Price = 8.20m, Status = "Low Stock" },
            new() { Code = "PK-001", Name = "Packaging Box", Type = "PACKAGING", UOM = "EA", Stock = 5000, Price = 0.50m, Status = "Active" },
        };
        await _db.Set<MaterialMasterEntity>().AddRangeAsync(materials);

        var vendors = new List<VendorEntity>
        {
            new() { Code = "VEN-001", Name = "ABC Supplies Ltd.", TaxId = "TX-12345", PaymentTerms = "Net 30", Phone = "+1-555-0201", Status = "Active" },
            new() { Code = "VEN-002", Name = "GlobalChem Industries", TaxId = "TX-12346", PaymentTerms = "Net 45", Phone = "+1-555-0202", Status = "Active" },
            new() { Code = "VEN-003", Name = "PackRight Corp.", TaxId = "TX-12347", PaymentTerms = "Net 30", Phone = "+1-555-0203", Status = "On Hold" },
        };
        await _db.Set<VendorEntity>().AddRangeAsync(vendors);

        var customers = new List<CustomerEntity>
        {
            new() { Code = "CUST-001", Name = "Acme Corporation", CreditLimit = 100000, PaymentTerms = "Net 30", Phone = "+1-555-0301", Status = "Active" },
            new() { Code = "CUST-002", Name = "Globex Industries", CreditLimit = 250000, PaymentTerms = "Net 45", Phone = "+1-555-0302", Status = "Active" },
            new() { Code = "CUST-003", Name = "Innotech Solutions", CreditLimit = 50000, PaymentTerms = "Net 30", Phone = "+1-555-0303", Status = "Credit Hold" },
        };
        await _db.Set<CustomerEntity>().AddRangeAsync(customers);

        var employees = new List<EmployeeEntity>
        {
            new() { Code = "EMP-001", Name = "John Doe", Department = "Production", Designation = "Plant Manager", Mobile = "+1-555-0101", Status = "Active" },
            new() { Code = "EMP-002", Name = "Jane Smith", Department = "Quality", Designation = "QC Supervisor", Mobile = "+1-555-0102", Status = "Active" },
        };
        await _db.Set<EmployeeEntity>().AddRangeAsync(employees);

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

        var boms = new List<BillOfMaterialEntity>
        {
            new() { BomId = "BOM-001", ProductName = "Finished Product A", ComponentName = "Raw Material X", Quantity = 2, UOM = "KG", Status = "Active" },
            new() { BomId = "BOM-002", ProductName = "Finished Product A", ComponentName = "Raw Material Y", Quantity = 0.5m, UOM = "KG", Status = "Active" },
            new() { BomId = "BOM-003", ProductName = "Finished Product A", ComponentName = "Packaging Box", Quantity = 1, UOM = "EA", Status = "Active" },
        };
        await _db.Set<BillOfMaterialEntity>().AddRangeAsync(boms);

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
