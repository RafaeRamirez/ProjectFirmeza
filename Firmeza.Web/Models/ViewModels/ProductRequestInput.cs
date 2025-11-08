using System;
using System.ComponentModel.DataAnnotations;

namespace Firmeza.Web.Models.ViewModels
{
    public class ProductRequestInput
    {
        [Required]
        public Guid ProductId { get; set; }

        [Range(1, 1000)]
        public int Quantity { get; set; } = 1;

        [StringLength(250)]
        public string? Note { get; set; }
    }
}
