using Microsoft.EntityFrameworkCore;

namespace Nexora.Infrastructure.Persistence;

public partial class AppDbContext
{
    public DbSet<Domain.Finance.FinancialAccount> FinancialAccounts => Set<Domain.Finance.FinancialAccount>();
    public DbSet<Domain.Finance.ExpenseCategory> ExpenseCategories => Set<Domain.Finance.ExpenseCategory>();
    public DbSet<Domain.Finance.FinancialEntry> FinancialEntries => Set<Domain.Finance.FinancialEntry>();
    public DbSet<Domain.Finance.Employee> Employees => Set<Domain.Finance.Employee>();
    public DbSet<Domain.Finance.Payroll> Payrolls => Set<Domain.Finance.Payroll>();
    public DbSet<Domain.Finance.PayrollItem> PayrollItems => Set<Domain.Finance.PayrollItem>();
}
