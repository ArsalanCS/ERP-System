using Erp.Application.Abstractions;
using Erp.Domain.Identity;
using Microsoft.EntityFrameworkCore;

namespace Erp.Infrastructure.Persistence.Repositories;

public sealed class EmployeeRepository(ErpDbContext context) : IEmployeeRepository
{
    public Task<Employee?> GetByUserIdAsync(long userId, CancellationToken cancellationToken = default)
        => context.Employees.FirstOrDefaultAsync(e => e.UserId == userId, cancellationToken);

    public async Task<IReadOnlyDictionary<long, Employee>> GetByUserIdsAsync(
        IReadOnlyCollection<long> userIds, CancellationToken cancellationToken = default)
    {
        if (userIds.Count == 0) return new Dictionary<long, Employee>();
        var rows = await context.Employees.AsNoTracking()
            .Where(e => userIds.Contains(e.UserId))
            .ToListAsync(cancellationToken);
        return rows.ToDictionary(e => e.UserId);
    }

    public void Add(Employee employee) => context.Employees.Add(employee);
}
