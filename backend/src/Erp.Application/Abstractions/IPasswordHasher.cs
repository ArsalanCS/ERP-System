namespace Erp.Application.Abstractions;

/// <summary>
/// Password hashing abstraction. The implementation standardizes on ASP.NET
/// Core Identity's PBKDF2 hasher (CLAUDE.md §4.5 decision). One algorithm only.
/// </summary>
public interface IPasswordHasher
{
    string Hash(string password);

    PasswordVerificationResult Verify(string hash, string providedPassword);
}

public enum PasswordVerificationResult
{
    Failed = 0,
    Success = 1,
    /// <summary>Valid, but the hash should be upgraded (e.g. iteration count bumped).</summary>
    SuccessRehashNeeded = 2,
}
