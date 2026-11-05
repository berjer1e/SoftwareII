/*
 * Файл: User.cs
 * Призначення: Сутність бази даних Entity Framework Core для таблиці User (користувачі системи).
 */

using System;
using System.Collections.Generic;

namespace CompanyDataAccessLayer.Entities;

public partial class User
{
    public Guid UserId { get; set; }

    public DateOnly RegDate { get; set; }

    public bool IsActive { get; set; }

    public Guid? UserRole { get; set; }

    public string? UserName { get; set; }

    public string? ContactEmail { get; set; }

    public string? PasswordHash { get; set; }

    public DateTime RowInsertTime { get; set; }

    public DateTime RowUpdateTime { get; set; }

    public decimal Balance { get; set; }

    public virtual ICollection<Auction> AuctionAuctionManagers { get; set; } = new List<Auction>();

    public virtual ICollection<Auction> AuctionProductOwners { get; set; } = new List<Auction>();

    public virtual ICollection<Bid> Bids { get; set; } = new List<Bid>();
}
