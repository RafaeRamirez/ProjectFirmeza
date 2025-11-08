namespace Firmeza.Web.Models.ViewModels
{
    public class ForgotPasswordConfirmationViewModel
    {
        public bool EmailSent { get; set; }
        public bool ShowResetLink { get; set; }
        public string? ResetLink { get; set; }
        public string? ErrorMessage { get; set; }
    }
}
