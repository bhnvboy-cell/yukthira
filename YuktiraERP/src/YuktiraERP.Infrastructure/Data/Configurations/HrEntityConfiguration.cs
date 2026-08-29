using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using YuktiraERP.Infrastructure.Data.Entities;

namespace YuktiraERP.Infrastructure.Data.Configurations;

public class EmployeeEntityConfiguration : IEntityTypeConfiguration<EmployeeEntity>
{
    public void Configure(EntityTypeBuilder<EmployeeEntity> builder)
    {
        builder.ToTable("employee_masters", "yuktira_hr");
        builder.Property(e => e.BasicSalary).HasColumnType("decimal(18,2)");
    }
}

public class LeaveRequestEntityConfiguration : IEntityTypeConfiguration<LeaveRequestEntity>
{
    public void Configure(EntityTypeBuilder<LeaveRequestEntity> builder)
    {
        builder.ToTable("leave_records", "yuktira_hr");
    }
}

public class PayrollEntryEntityConfiguration : IEntityTypeConfiguration<PayrollEntryEntity>
{
    public void Configure(EntityTypeBuilder<PayrollEntryEntity> builder)
    {
        builder.ToTable("payroll_entries", "yuktira_hr");
        builder.Property(e => e.GrossPay).HasColumnType("decimal(18,2)");
        builder.Property(e => e.Deductions).HasColumnType("decimal(18,2)");
        builder.Property(e => e.NetPay).HasColumnType("decimal(18,2)");
    }
}

public class AttendanceEntityConfiguration : IEntityTypeConfiguration<AttendanceEntity>
{
    public void Configure(EntityTypeBuilder<AttendanceEntity> builder)
    {
        builder.ToTable("attendance_records", "yuktira_hr");
    }
}

public class AppraisalEntityConfiguration : IEntityTypeConfiguration<AppraisalEntity>
{
    public void Configure(EntityTypeBuilder<AppraisalEntity> builder)
    {
        builder.ToTable("appraisals", "yuktira_hr");
    }
}

public class OrgUnitEntityConfiguration : IEntityTypeConfiguration<OrgUnitEntity>
{
    public void Configure(EntityTypeBuilder<OrgUnitEntity> builder)
    {
        builder.ToTable("org_units", "yuktira_hr");
    }
}

public class TimeEntryEntityConfiguration : IEntityTypeConfiguration<TimeEntryEntity>
{
    public void Configure(EntityTypeBuilder<TimeEntryEntity> builder)
    {
        builder.ToTable("time_entries", "yuktira_hr");
        builder.Property(e => e.Hours).HasColumnType("decimal(5,2)");
    }
}

public class RecruitmentEntityConfiguration : IEntityTypeConfiguration<RecruitmentEntity>
{
    public void Configure(EntityTypeBuilder<RecruitmentEntity> builder)
    {
        builder.ToTable("recruitments", "yuktira_hr");
        builder.Property(e => e.MinSalary).HasColumnType("decimal(18,2)");
        builder.Property(e => e.MaxSalary).HasColumnType("decimal(18,2)");
    }
}
