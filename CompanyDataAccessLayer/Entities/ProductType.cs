using System;
using System.Collections.Generic;

namespace CompanyDataAccessLayer.Entities;

public partial class ProductType
{
    public Guid TypeId { get; set; }

    public string? TypeName { get; set; }

    public virtual ICollection<Product> Products { get; set; } = new List<Product>();
}
