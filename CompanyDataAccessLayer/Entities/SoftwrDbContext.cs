/*
 * Файл: SoftwrDbContext.cs
 * Призначення: Контекст бази даних Entity Framework Core для підключення до MS SQL Server (softwrDB)
 * та конфігурації таблиць, обмежень, зовнішніх ключів і тригерів.
 */

using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;

namespace CompanyDataAccessLayer.Entities;

public partial class SoftwrDbContext : DbContext
{
    /*
     * Ініціалізує контекст бази даних SoftwrDbContext із заданими параметрами DbContextOptions.
     */
    public SoftwrDbContext(DbContextOptions<SoftwrDbContext> options)
        : base(options)
    {
    }

    public virtual DbSet<Auction> Auctions { get; set; }

    public virtual DbSet<Bid> Bids { get; set; }

    public virtual DbSet<Manufacturer> Manufacturers { get; set; }

    public virtual DbSet<Product> Products { get; set; }

    public virtual DbSet<ProductType> ProductTypes { get; set; }

    public virtual DbSet<User> Users { get; set; }

    /*
     * Налаштовує схему таблиць, типи стовпців, значення за замовчуванням та зв'язки між сутностями бази даних.
     */
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Auction>(entity =>
        {
            entity.ToTable("Auction", tb => tb.HasTrigger("TR_Auction_RowUpdateTime"));

            entity.Property(e => e.AuctionId)
                .HasDefaultValueSql("(newid())")
                .HasColumnName("AuctionID");
            entity.Property(e => e.AuctionManagerId).HasColumnName("AuctionManagerID");
            entity.Property(e => e.BuyoutPrice)
                .HasDefaultValueSql("((1.00))")
                .HasColumnType("decimal(10, 2)");
            entity.Property(e => e.EndDate).HasColumnType("datetime");
            entity.Property(e => e.FinalPrice).HasColumnType("decimal(10, 2)");
            entity.Property(e => e.IsVerified).HasDefaultValue(true);
            entity.Property(e => e.LastBidId).HasColumnName("LastBidID");
            entity.Property(e => e.MinBid)
                .HasDefaultValueSql("((0.01))")
                .HasColumnType("decimal(10, 2)");
            entity.Property(e => e.ProductId).HasColumnName("ProductID");
            entity.Property(e => e.ProductOwnerId).HasColumnName("ProductOwnerID");
            entity.Property(e => e.RowInsertTime).HasDefaultValueSql("(sysutcdatetime())");
            entity.Property(e => e.RowUpdateTime).HasDefaultValueSql("(sysutcdatetime())");
            entity.Property(e => e.StartDate).HasColumnType("datetime");
            entity.Property(e => e.StartPrice).HasColumnType("decimal(10, 2)");

            entity.HasOne(d => d.AuctionManager).WithMany(p => p.AuctionAuctionManagers)
                .HasForeignKey(d => d.AuctionManagerId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Auction_User_Manager");

            entity.HasOne(d => d.LastBid).WithMany(p => p.Auctions)
                .HasForeignKey(d => d.LastBidId)
                .HasConstraintName("FK_Auction_Bid_LastBid");

            entity.HasOne(d => d.Product).WithMany(p => p.Auctions)
                .HasForeignKey(d => d.ProductId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Auction_Products");

            entity.HasOne(d => d.ProductOwner).WithMany(p => p.AuctionProductOwners)
                .HasForeignKey(d => d.ProductOwnerId)
                .HasConstraintName("FK_Auction_User_Owner");
        });

        modelBuilder.Entity<Bid>(entity =>
        {
            entity.ToTable("Bid");

            entity.Property(e => e.BidId)
                .HasDefaultValueSql("(newid())")
                .HasColumnName("BidID");
            entity.Property(e => e.AuctionId).HasColumnName("AuctionID");
            entity.Property(e => e.Price).HasColumnType("decimal(10, 2)");
            entity.Property(e => e.RowInsertTime).HasDefaultValueSql("(sysutcdatetime())");
            entity.Property(e => e.RowUpdateTime).HasDefaultValueSql("(sysutcdatetime())");
            entity.Property(e => e.TimeStamp).HasDefaultValueSql("(sysutcdatetime())");

            entity.HasOne(d => d.Auction).WithMany(p => p.Bids)
                .HasForeignKey(d => d.AuctionId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Bid_Auction");

            entity.HasOne(d => d.BidOwnerNavigation).WithMany(p => p.Bids)
                .HasForeignKey(d => d.BidOwner)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Bid_User");
        });

        modelBuilder.Entity<Manufacturer>(entity =>
        {
            entity.Property(e => e.ManufacturerId)
                .HasDefaultValueSql("(newid())")
                .HasColumnName("ManufacturerID");
            entity.Property(e => e.Country).HasMaxLength(50);
            entity.Property(e => e.Name).HasMaxLength(100);
        });

        modelBuilder.Entity<Product>(entity =>
        {
            entity.HasKey(e => e.ProductId).HasName("PK__tmp_ms_x__B40CC6CD10C3D752");

            entity.Property(e => e.ProductId)
                .HasDefaultValueSql("(newid())")
                .HasColumnName("ProductID");
            entity.Property(e => e.BasePrice).HasColumnType("decimal(10, 2)");
            entity.Property(e => e.ManufacturerId).HasColumnName("ManufacturerID");
            entity.Property(e => e.ProductName).HasMaxLength(50);
            entity.Property(e => e.ProductTypeId).HasColumnName("ProductTypeID");

            entity.HasOne(d => d.Manufacturer).WithMany(p => p.Products)
                .HasForeignKey(d => d.ManufacturerId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Products_Manufacturers");

            entity.HasOne(d => d.ProductType).WithMany(p => p.Products)
                .HasForeignKey(d => d.ProductTypeId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Products_ProductType");
        });

        modelBuilder.Entity<ProductType>(entity =>
        {
            entity.HasKey(e => e.TypeId);

            entity.ToTable("ProductType");

            entity.Property(e => e.TypeId)
                .HasDefaultValueSql("(newid())")
                .HasColumnName("TypeID");
            entity.Property(e => e.TypeName).HasMaxLength(50);
        });

        modelBuilder.Entity<User>(entity =>
        {
            entity.ToTable("User", tb => tb.HasTrigger("TR_User_RowUpdateTime"));

            entity.Property(e => e.UserId)
                .HasDefaultValueSql("(newid())")
                .HasColumnName("UserID");
            entity.Property(e => e.Balance)
                .HasDefaultValueSql("((1000.00))")
                .HasColumnType("decimal(12, 2)");
            entity.Property(e => e.ContactEmail).HasMaxLength(100);
            entity.Property(e => e.PasswordHash).HasMaxLength(256);
            entity.Property(e => e.RowInsertTime).HasDefaultValueSql("(sysutcdatetime())");
            entity.Property(e => e.RowUpdateTime).HasDefaultValueSql("(sysutcdatetime())");
            entity.Property(e => e.UserName).HasMaxLength(50);
        });

        OnModelCreatingPartial(modelBuilder);
    }

    /*
     * Частковий метод для додаткового розширення конфігурації моделі.
     */
    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
