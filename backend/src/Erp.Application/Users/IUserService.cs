using Erp.Application.Common;
using Erp.Shared.Results;

namespace Erp.Application.Users;

public interface IUserService
{
    Task<Result<PagedResult<UserListItem>>> ListAsync(UserListQuery query, CancellationToken cancellationToken = default);
    Task<Result<UserDetail>> GetAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Result<CreateUserResult>> CreateAsync(CreateUserRequest request, CancellationToken cancellationToken = default);
    Task<Result> UpdateAsync(Guid id, UpdateUserRequest request, CancellationToken cancellationToken = default);
    Task<Result> SuspendAsync(Guid id, SuspendUserRequest request, CancellationToken cancellationToken = default);
    Task<Result> ReactivateAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Result> ArchiveAsync(Guid id, CancellationToken cancellationToken = default);
}
