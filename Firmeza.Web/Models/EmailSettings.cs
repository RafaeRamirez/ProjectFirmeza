namespace Firmeza.Web.Models
{
    public class EmailSettings
    {
        public string DisplayName { get; set; } = "Firmeza";
        public string From { get; set; } = string.Empty;
        public string Host { get; set; } = string.Empty;
        public int Port { get; set; } = 587;
        public string User { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public bool EnableSsl { get; set; } = true;
    }
}
