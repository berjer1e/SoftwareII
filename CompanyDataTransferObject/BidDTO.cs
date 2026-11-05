/*
 * Файл: BidDTO.cs
 * Призначення: Об'єкт передачі даних для ставки або миттєвого викупу на аукціоні.
 * Зберігає суму ставки, час подання, прапорець викупу та дані учасника торгів.
 */

using System;

namespace CompanyDataTransferObject
{
    public class BidDTO : BaseDTO
    {
        public Guid AuctionId { get; set; }
        public Guid BidOwner { get; set; }
        public decimal Price { get; set; }
        public DateTime TimeStamp { get; set; } = DateTime.UtcNow;
        public bool IsBuyout { get; set; }

        public DateTime RowInsertTime { get; set; } = DateTime.UtcNow;
        public DateTime? RowUpdateTime { get; set; }

        public string? BidderName { get; set; }
        public string? BidderEmail { get; set; }
    }
}
