using Erp.Application.Abstractions;
using Microsoft.AspNetCore.Identity;
using AppResult = Erp.Application.Abstractions.PasswordVerificationResult;
using IdentityResult = Microsoft.AspNetCore.Identity.PasswordVerificationResult;

namespace Erp.Infrastructure.Identity;

/// <summary>
/// The platform's single password-hashing implementation: ASP.NET Core
/// Identity's PBKDF2 hasher (CLAUDE.md §4.5 decision). Do not mix algorithms.
/// </summary>
public sealed class Pbkdf2PasswordHasher : IPasswordHasher
{
    private static readonly object Dummy = new();
    private readonly PasswordHasher<object> _hasher = new();

    public string Hash(string password) => _hasher.HashPassword(Dummy, password);

    public AppResult Verify(string hash, string providedPassword)
    {
        if (string.IsNullOrEmpty(hash))
        {
            return AppResult.Failed;
        }

        try
        {
            return _hasher.VerifyHashedPassword(Dummy, hash, providedPassword) switch
            {
                IdentityResult.Success => AppResult.Success,
                IdentityResult.SuccessRehashNeeded => AppResult.SuccessRehashNeeded,
                _ => AppResult.Failed,
            };
        }
        catch (FormatException)
        {
            return AppResult.Failed;
        }
    }
}
