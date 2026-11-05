/*
 * Файл: Bid.cs
 * Призначення: Сутність бази даних Entity Framework Core для таблиці Bid (ставки на аукціонах).
 */

using System;
using System.Collections.Generic;

namespace CompanyDataAccessLayer.Entities;

public partial class Bid
{
    public Guid BidId { get; set; }

    public Guid AuctionId { get; set; }

    public Guid BidOwner { get; set; }

    public decimal Price { get; set; }

    public DateTime TimeStamp { get; set; }

    public bool IsBuyout { get; set; }

    public DateTime RowInsertTime { get; set; }

    public DateTime RowUpdateTime { get; set; }

    public virtual Auction Auction { get; set; } = null!;

    public virtual ICollection<Auction> Auctions { get; set; } = new List<Auction>();

    public virtual User BidOwnerNavigation { get; set; } = null!;
}
