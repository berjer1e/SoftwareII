/*
 * Файл: AuctionDTO.cs
 * Призначення: Об'єкт передачі даних для сутності аукціону (лота). Містить основні цінові й часові параметри,
 * статус верифікації менеджером, інформацію про останню ставку та обчислювані властивості для інтерфейсу WPF.
 */

using System;

namespace CompanyDataTransferObject
{
    public class AuctionDTO : BaseDTO
    {
        public Guid ProductId { get; set; }
        public Guid AuctionManagerId { get; set; }
        public Guid? ProductOwnerId { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public decimal StartPrice { get; set; }
        public decimal EndPrice { get; set; }
        public decimal MinBid { get; set; } = 0.01m;
        public decimal BuyoutPrice { get; set; } = 1.00m;
        public bool IsVerified { get; set; } = true;
        public bool IsActive { get; set; } = true;
        public Guid? LastBidId { get; set; }

        public DateTime RowInsertTime { get; set; } = DateTime.UtcNow;
        public DateTime? RowUpdateTime { get; set; }

        public string? ProductName { get; set; }
        public string? ProductTypeName { get; set; }
        public string? ManufacturerName { get; set; }
        public string? ManagerName { get; set; }
        public string? AuctionManagerName { get => ManagerName; set => ManagerName = value; }
        public string? ManagerEmail { get; set; }
        public string? ProductOwnerName { get; set; }
        public string? LastBidderName { get; set; }
        public decimal? ProductBasePrice { get; set; }

        public bool IsFinished => !IsActive || EndDate < DateTime.Now;
        public decimal MinNextBid => EndPrice > 0 ? EndPrice + MinBid : StartPrice;
        public string StatusText => !IsVerified
            ? "Очікує підтвердження"
            : (!IsActive ? "Деактивовано" : (EndDate < DateTime.Now ? "Завершено" : "Активний"));
    }
}
