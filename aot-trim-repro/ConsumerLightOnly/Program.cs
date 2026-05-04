// This app ONLY uses the light cross-origin types.
// If trimming works, the heavy crypto types (RSA, AES, etc.) should be stripped.

using HeavyLight.Light;

var options = new CrossOriginProtectionOptions();
var protection = new DefaultCrossOriginProtection(options);

// Test same-origin — should be allowed
var result1 = protection.Validate("same-origin", null, "https://mysite.com");
Console.WriteLine($"same-origin: {result1}");

// Test cross-site — should be denied
var result2 = protection.Validate("cross-site", "https://evil.com", "https://mysite.com");
Console.WriteLine($"cross-site:  {result2}");

// Test no headers (non-browser) — should be allowed
var result3 = protection.Validate("", null, "https://mysite.com");
Console.WriteLine($"non-browser: {result3}");

Console.WriteLine("Light-only consumer completed successfully.");
