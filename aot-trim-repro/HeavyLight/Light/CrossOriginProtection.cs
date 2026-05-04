// Simulates cross-origin protection — a "light" type with ZERO crypto dependencies.

namespace HeavyLight.Light;

/// <summary>
/// Simulates CrossOriginAntiforgeryResult.
/// </summary>
public enum CrossOriginResult
{
    Allowed,
    Denied
}

/// <summary>
/// Simulates ICrossOriginProtection.
/// </summary>
public interface ICrossOriginProtection
{
    CrossOriginResult Validate(string secFetchSite, string? origin, string requestHost);
}

/// <summary>
/// Simulates CrossOriginProtectionOptions.
/// </summary>
public class CrossOriginProtectionOptions
{
    public IList<string> TrustedOrigins { get; } = new List<string>();
}

/// <summary>
/// Simulates DefaultCrossOriginProtection — pure header logic, no crypto.
/// </summary>
public class DefaultCrossOriginProtection : ICrossOriginProtection
{
    private readonly string[] _trustedOrigins;

    public DefaultCrossOriginProtection(CrossOriginProtectionOptions options)
    {
        _trustedOrigins = options.TrustedOrigins.ToArray();
    }

    public CrossOriginResult Validate(string secFetchSite, string? origin, string requestHost)
    {
        if (origin is not null)
        {
            foreach (var trusted in _trustedOrigins)
            {
                if (string.Equals(origin, trusted, StringComparison.OrdinalIgnoreCase))
                {
                    return CrossOriginResult.Allowed;
                }
            }
        }

        if (string.Equals(secFetchSite, "same-origin", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(secFetchSite, "none", StringComparison.OrdinalIgnoreCase))
        {
            return CrossOriginResult.Allowed;
        }

        if (string.Equals(secFetchSite, "cross-site", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(secFetchSite, "same-site", StringComparison.OrdinalIgnoreCase))
        {
            return CrossOriginResult.Denied;
        }

        if (origin is not null && string.Equals(origin, requestHost, StringComparison.OrdinalIgnoreCase))
        {
            return CrossOriginResult.Allowed;
        }

        if (string.IsNullOrEmpty(secFetchSite) && origin is null)
        {
            return CrossOriginResult.Allowed;
        }

        return CrossOriginResult.Denied;
    }
}
