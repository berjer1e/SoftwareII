/*
 * Файл: ProductDTO.cs
 * Призначення: Об'єкт передачі даних товару, який виставляється на аукціон.
 * Містить назву, базову ціну, кількість, а також посилання на категорію та виробника.
 */

using System;

namespace CompanyDataTransferObject
{
    public class ProductDTO : BaseDTO
    {
        public string Name { get; set; } = string.Empty;
        public Guid ProductTypeId { get; set; }
        public int Quantity { get; set; }
        public decimal Price { get; set; } = 0;
        public Guid ManufacturerId { get; set; }

        public string? ProductTypeName { get; set; }
        public string? ManufacturerName { get; set; }
    }
}
