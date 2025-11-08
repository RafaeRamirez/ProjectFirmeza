using System;
using System.ComponentModel.DataAnnotations;

namespace Firmeza.Web.Models.ViewModels
{
    public class ReviewProductRequestInput
    {
        [Required]
        public Guid RequestId { get; set; }

        [Required]
        [RegularExpression("^(approve|reject)$")]
        public string Action { get; set; } = "approve";

        [StringLength(500)]
        public string? ResponseMessage { get; set; }
    }
}
