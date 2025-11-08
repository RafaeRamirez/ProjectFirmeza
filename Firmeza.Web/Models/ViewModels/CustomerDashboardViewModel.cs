using System.Collections.Generic;
using Firmeza.Web.Models;

namespace Firmeza.Web.Models.ViewModels
{
    public class CustomerDashboardViewModel
    {
        public List<ProductRequest> Requests { get; set; } = new();
        public bool HasRecentUpdates { get; set; }
    }
}
