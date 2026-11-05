/*
 * Файл: Auction.cs
 * Призначення: Сутність бази даних Entity Framework Core для таблиці Auction (аукціони та лоти).
 */

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

    public DateTime RowInsertTime { get; set; }

    public DateTime RowUpdateTime { get; set; }

    public decimal MinBid { get; set; }

    public decimal BuyoutPrice { get; set; }

    public Guid? ProductOwnerId { get; set; }

    public bool IsVerified { get; set; }

    public Guid? LastBidId { get; set; }

    public virtual User AuctionManager { get; set; } = null!;

    public virtual ICollection<Bid> Bids { get; set; } = new List<Bid>();

    public virtual Bid? LastBid { get; set; }

    public virtual Product Product { get; set; } = null!;

    public virtual User? ProductOwner { get; set; }
}
