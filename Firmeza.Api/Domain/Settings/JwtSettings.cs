namespace Firmeza.Api.Domain.Settings;

public class JwtSettings
{
    public string Issuer { get; set; } = "Firmeza.Api";
    public string Audience { get; set; } = "Firmeza.Client";
    public string SigningKey { get; set; } = string.Empty;
    public int ExpirationMinutes { get; set; } = 60;
}
