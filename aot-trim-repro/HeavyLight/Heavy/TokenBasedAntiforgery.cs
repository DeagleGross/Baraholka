// Simulates token-based antiforgery — depends on CryptoLib (DataProtection).
// This is like DefaultAntiforgeryTokenSerializer depending on IDataProtector.

using CryptoLib;

namespace HeavyLight.Heavy;

/// <summary>
/// Simulates token-based antiforgery — depends on IDataProtector (heavy).
/// </summary>
public class TokenBasedAntiforgery
{
    private readonly IDataProtector _protector;

    public TokenBasedAntiforgery(IDataProtectionProvider provider)
    {
        _protector = provider.CreateProtector("Antiforgery.Token.v1");
    }

    public string GenerateToken()
    {
        var data = System.Security.Cryptography.RandomNumberGenerator.GetBytes(32);
        return Convert.ToBase64String(_protector.Protect(data));
    }

    public bool ValidateToken(string token)
    {
        var data = _protector.Unprotect(Convert.FromBase64String(token));
        return data.Length > 0;
    }
}

