using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CompanyDataTransferObject
{
    public class ProductDTO: BaseDTO
    {
     
        public string Name { get; set; } = string.Empty;
        public Guid ProductTypeId { get; set; }
        public int Quantity { get; set; }
        public decimal Price { get; set; } = 0;
        public Guid ManufacturerId { get; set; }
    }
}
