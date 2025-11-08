using System;
using System.Collections.Generic;

namespace Firmeza.Web.Models
{
    public class ProductDeleteResult
    {
        public bool Removed { get; set; }
        public bool SetInactive { get; set; }
        public bool HasSales { get; set; }
        public List<Guid> DeletedSales { get; } = new();
    }
}
