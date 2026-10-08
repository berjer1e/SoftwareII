using System;
using System.Collections.Generic;

namespace CompanyDataAccessLayer.Entities;

public partial class Product
{
    public Guid ProductId { get; set; }

    public string? ProductName { get; set; }

    public Guid ProductTypeId { get; set; }

    public int Quantity { get; set; }

    public decimal? BasePrice { get; set; } 

    public Guid ManufacturerId { get; set; }

    public virtual ICollection<Auction> Auctions { get; set; } = new List<Auction>();

    public virtual Manufacturer Manufacturer { get; set; } = null!;

    public virtual ProductType ProductType { get; set; } = null!;
}
