namespace Firmeza.Web.Models.ViewModels;

public class AdminUserListItemViewModel
{
    public string UserId { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public IReadOnlyList<string> Roles { get; set; } = Array.Empty<string>();
}
