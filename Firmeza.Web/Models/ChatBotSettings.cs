namespace Firmeza.Web.Models
{
    public class ChatBotSettings
    {
        public int Id { get; set; }
        public string ApiKey { get; set; } = string.Empty;
        public string Model { get; set; } = "models/gemini-1.5-flash";
        public string Scope { get; set; } = "https://www.googleapis.com/auth/generative-language";
        public string ServiceAccountJsonPath { get; set; } = string.Empty;
        public string Endpoint { get; set; } = "https://generativelanguage.googleapis.com";
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }
}
