using System;
using System.Collections.Generic;
using CompanyDataTransferObject;
using CompanyDataTransferObject.Roles;

namespace CompanyServiceLayer;

public interface IAuctionService
{
    List<AuctionDTO> GetAllAuctions();
    AuctionDTO? GetAuctionById(Guid id);
    List<UserDTO> GetAllUsers();
    UserDTO? GetUserById(Guid id);
    List<ProductDTO> GetAllProducts();
    bool CloseAuction(Guid auctionId);
    bool AddAuction<TRole>(AuctionDTO auction) where TRole : ICanManageAuctions;
    bool AddAuction(AuctionDTO auction);
    bool UpdateBalance(Guid userId, decimal newBalance);
    bool BuyoutAuction(Guid auctionId, Guid buyerId, decimal commissionPercent = 0.10m);
    bool PlaceBid(Guid auctionId, decimal bidAmount);
}
