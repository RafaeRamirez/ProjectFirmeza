using Firmeza.WebApplication.Interfaces;

namespace Firmeza.WebApplication.Utils;

/// <summary>
/// Trims null/whitespace safely.
/// </summary>
public class StringSanitizer : IStringSanitizer
{
    public string Clean(string? input) => (input ?? string.Empty).Trim();
}
