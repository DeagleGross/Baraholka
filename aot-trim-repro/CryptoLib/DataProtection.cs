// Simulates Microsoft.AspNetCore.DataProtection — a separate assembly
// that brings heavy crypto dependencies (RSA, AES, key management, etc.)

using System.Security.Cryptography;

namespace CryptoLib;

/// <summary>
/// Simulates IDataProtectionProvider.
/// </summary>
public interface IDataProtectionProvider
{
    IDataProtector CreateProtector(string purpose);
}

/// <summary>
/// Simulates IDataProtector.
/// </summary>
public interface IDataProtector
{
    byte[] Protect(byte[] plaintext);
    byte[] Unprotect(byte[] protectedData);
}

/// <summary>
/// Heavy implementation — pulls RSA, AES, key derivation.
/// </summary>
public class DefaultDataProtectionProvider : IDataProtectionProvider
{
    private readonly RSA _masterKey = RSA.Create(2048);

    public IDataProtector CreateProtector(string purpose)
    {
        return new DefaultDataProtector(_masterKey, purpose);
    }
}

public class DefaultDataProtector : IDataProtector
{
    private readonly RSA _rsa;
    private readonly byte[] _purposeBytes;
    private readonly Aes _aes = Aes.Create();

    public DefaultDataProtector(RSA rsa, string purpose)
    {
        _rsa = rsa;
        _purposeBytes = System.Text.Encoding.UTF8.GetBytes(purpose);
    }

    public byte[] Protect(byte[] plaintext)
    {
        using var hmac = new HMACSHA256(_purposeBytes);
        var derived = hmac.ComputeHash(plaintext);
        return _aes.EncryptCbc(plaintext, derived.AsSpan(0, 16).ToArray());
    }

    public byte[] Unprotect(byte[] protectedData)
    {
        return _aes.DecryptCbc(protectedData, _aes.IV);
    }
}
