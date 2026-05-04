// This app uses BOTH heavy and light types.
// The binary should be larger due to CryptoLib (DataProtection) being pulled in.

using HeavyLight.Light;
using HeavyLight.Heavy;
using CryptoLib;

// Light: cross-origin
var options = new CrossOriginProtectionOptions();
var protection = new DefaultCrossOriginProtection(options);
var result = protection.Validate("same-origin", null, "https://mysite.com");
Console.WriteLine($"Cross-origin: {result}");

// Heavy: token-based (pulls in CryptoLib = DataProtection)
var provider = new DefaultDataProtectionProvider();
var antiforgery = new TokenBasedAntiforgery(provider);
var token = antiforgery.GenerateToken();
Console.WriteLine($"Token generated: {token[..20]}...");

Console.WriteLine("Both consumer completed successfully.");
