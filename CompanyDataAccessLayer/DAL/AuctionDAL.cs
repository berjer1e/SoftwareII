using CompanyDataTransferObject;
using Ent = CompanyDataAccessLayer.Entities;

namespace CompanyDataAccessLayer.DAL
{
    public interface IAuctionDAL : IBaseDAL<AuctionDTO>
    {
        List<AuctionDTO> GetActive();
    }

    public class AuctionDAL : BaseDAL<Ent.Auction, AuctionDTO>, IAuctionDAL
    {
        public AuctionDAL(string connectionString) : base(connectionString) { }

        public List<AuctionDTO> GetActive()
        {
            using var db = CreateContext();
            return db.Auctions.Where(a => a.IsActive).ToList().Select(ToDto).ToList();
        }

        protected override AuctionDTO ToDto(Ent.Auction e) => new()
        {
            Id = e.AuctionId,
            ProductId = e.ProductId,
            AuctionManagerId = e.AuctionManagerId,
            StartDate = e.StartDate,
            EndDate = e.EndDate,
            StartPrice = e.StartPrice,
            EndPrice = e.FinalPrice,
            IsActive = e.IsActive
        };

        protected override Ent.Auction ToEntity(AuctionDTO d) => new()
        {
            AuctionId = d.Id,
            ProductId = d.ProductId,
            AuctionManagerId = d.AuctionManagerId,
            StartDate = d.StartDate,
            EndDate = d.EndDate,
            StartPrice = d.StartPrice,
            FinalPrice = d.EndPrice,
            IsActive = d.IsActive
        };
    }
}
