using System;
using System.Collections.Generic;

namespace CompanyDataAccessLayer.Entities;

public partial class Manufacturer
{
    public Guid ManufacturerId { get; set; }

    public string? Name { get; set; }

    public string Country { get; set; } = null!;

    public virtual ICollection<Product> Products { get; set; } = new List<Product>();
}
