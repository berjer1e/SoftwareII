using System;
using System.Collections.Generic;

namespace CompanyDataAccessLayer.Entities;

public partial class Auction
{
    public Guid AuctionId { get; set; }

    public Guid ProductId { get; set; }

    public Guid AuctionManagerId { get; set; }

    public DateTime StartDate { get; set; }

    public DateTime EndDate { get; set; }

    public decimal StartPrice { get; set; }

    public decimal FinalPrice { get; set; }

    public bool IsActive { get; set; }

    public virtual User AuctionManager { get; set; } = null!;

    public virtual Product Product { get; set; } = null!;
}
