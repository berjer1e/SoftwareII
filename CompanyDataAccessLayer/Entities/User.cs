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

    public virtual ICollection<Auction> Auctions { get; set; } = new List<Auction>();

    public virtual Role? UserRoleNavigation { get; set; }
}
