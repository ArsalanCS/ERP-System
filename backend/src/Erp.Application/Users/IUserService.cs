using Erp.Application.Common;
using Erp.Shared.Results;

namespace Erp.Application.Users;

public interface IUserService
{
    Task<Result<PagedResult<UserListItem>>> ListAsync(UserListQuery query, CancellationToken cancellationToken = default);
    Task<Result<UserDetail>> GetAsync(long id, CancellationToken cancellationToken = default);
    Task<Result<CreateUserResult>> CreateAsync(CreateUserRequest request, CancellationToken cancellationToken = default);
    Task<Result> UpdateAsync(long id, UpdateUserRequest request, CancellationToken cancellationToken = default);
    Task<Result> SuspendAsync(long id, SuspendUserRequest request, CancellationToken cancellationToken = default);
    Task<Result> ReactivateAsync(long id, CancellationToken cancellationToken = default);
    Task<Result> ArchiveAsync(long id, CancellationToken cancellationToken = default);
}
