using Erp.Application.Abstractions;
using Erp.Domain.Identity;
using Microsoft.EntityFrameworkCore;

namespace Erp.Infrastructure.Persistence.Repositories;

public sealed class EmployeeRepository(ErpDbContext context) : IEmployeeRepository
{
    public Task<Employee?> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default)
        => context.Employees.FirstOrDefaultAsync(e => e.UserId == userId, cancellationToken);

    public async Task<IReadOnlyDictionary<Guid, Employee>> GetByUserIdsAsync(
        IReadOnlyCollection<Guid> userIds, CancellationToken cancellationToken = default)
    {
        if (userIds.Count == 0) return new Dictionary<Guid, Employee>();
        var rows = await context.Employees.AsNoTracking()
            .Where(e => userIds.Contains(e.UserId))
            .ToListAsync(cancellationToken);
        return rows.ToDictionary(e => e.UserId);
    }

    public void Add(Employee employee) => context.Employees.Add(employee);
}
