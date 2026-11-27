using System;
using System.Collections.Generic;
using CompanyDataAccessLayer.DAL;
using CompanyDataTransferObject;
using CompanyDataTransferObject.Roles;

namespace CompanyServiceLayer.Session;

public class AuctionService : IAuctionService
{
    private readonly IAuctionDAL _auctionDal;
    private readonly IUserSession _userSession;
    private readonly IUserDAL _userDal;
    private readonly IProductDAL? _productDal;

    public AuctionService(IAuctionDAL auctionDal, IUserSession userSession, IUserDAL userDal, IProductDAL? productDal = null)
    {
        _auctionDal = auctionDal;
        _userSession = userSession;
        _userDal = userDal;
        _productDal = productDal;
    }

    public List<AuctionDTO> GetAllAuctions()
    {
        return _auctionDal.GetAll();
    }

    public AuctionDTO? GetAuctionById(Guid id)
    {
        return _auctionDal.GetById(id);
    }

    public List<UserDTO> GetAllUsers()
    {
        return _userDal.GetAll();
    }

    public UserDTO? GetUserById(Guid id)
    {
        return _userDal.GetById(id);
    }

    public List<ProductDTO> GetAllProducts()
    {
        return _productDal?.GetAll() ?? new List<ProductDTO>();
    }

    public bool CloseAuction(Guid auctionId)
    {
        var currentUser = _userSession.CurrentUser;
        if (currentUser == null || !currentUser.IsManager)
        {
            return false;
        }

        var auction = _auctionDal.GetById(auctionId);
        if (auction == null || !auction.IsActive)
        {
            return false;
        }

        auction.IsActive = false;
        if (auction.EndDate > DateTime.Now)
        {
            auction.EndDate = DateTime.Now;
        }

        return _auctionDal.Update(auction);
    }

    public bool AddAuction<TRole>(AuctionDTO auction) where TRole : ICanManageAuctions
    {
        return AddAuction(auction);
    }

    public bool AddAuction(AuctionDTO auction)
    {
        var currentUser = _userSession.CurrentUser;
        if (currentUser == null || !currentUser.IsManager)
        {
            return false;
        }

        auction.AuctionManagerId = currentUser.Id;
        auction.IsActive = true;

        return _auctionDal.Add(auction);
    }

    public bool UpdateBalance(Guid userId, decimal newBalance)
    {
        if (newBalance < 0)
        {
            return false;
        }

        var user = _userDal.GetById(userId);
        if (user == null)
        {
            return false;
        }

        user.Balance = newBalance;
        bool updated = _userDal.Update(user);

        if (updated && _userSession.CurrentUser?.Id == userId)
        {
            _userSession.CurrentUser.Balance = newBalance;
        }

        return updated;
    }

    public bool BuyoutAuction(Guid auctionId, Guid buyerId, decimal commissionPercent = 0.10m)
    {
        var auction = _auctionDal.GetById(auctionId);
        if (auction == null || !auction.IsActive)
        {
            return false;
        }

        decimal buyoutPrice = auction.EndPrice > 0 ? auction.EndPrice : auction.StartPrice;
        var buyer = _userDal.GetById(buyerId);
        if (buyer == null || buyer.Balance < buyoutPrice)
        {
            return false;
        }

        decimal commission = Math.Round(buyoutPrice * commissionPercent, 2);
        decimal sellerAmount = buyoutPrice - commission;

        buyer.Balance -= buyoutPrice;
        _userDal.Update(buyer);

        var seller = _userDal.GetById(auction.AuctionManagerId);
        if (seller != null)
        {
            seller.Balance += sellerAmount;
            _userDal.Update(seller);
        }

        var currentManagerId = _userSession.CurrentUser?.Id;
        if (currentManagerId != null)
        {
            var manager = _userDal.GetById(currentManagerId.Value);
            if (manager != null)
            {
                manager.Balance += commission;
                _userDal.Update(manager);

                if (_userSession.CurrentUser != null)
                {
                    _userSession.CurrentUser.Balance = manager.Balance;
                }
            }
        }

        auction.EndPrice = buyoutPrice;
        auction.IsActive = false;
        return _auctionDal.Update(auction);
    }

    public bool PlaceBid(Guid auctionId, decimal bidAmount)
    {
        var currentUser = _userSession.CurrentUser;
        if (currentUser == null)
        {
            return false;
        }

        if (currentUser.IsManager)
        {
            return false;
        }

        var auction = _auctionDal.GetById(auctionId);
        if (auction == null || !auction.IsActive)
        {
            return false;
        }

        if (auction.AuctionManagerId == currentUser.Id)
        {
            return false;
        }

        decimal currentPrice = auction.EndPrice > 0 ? auction.EndPrice : auction.StartPrice;
        if (bidAmount <= currentPrice)
        {
            return false;
        }

        auction.EndPrice = bidAmount;
        return _auctionDal.Update(auction);
    }
}
