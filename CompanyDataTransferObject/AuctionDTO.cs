using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CompanyDataTransferObject
{
    public class AuctionDTO: BaseDTO
    {
        
        public Guid ProductId { get; set; }
        public Guid AuctionManagerId { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public Decimal StartPrice { get; set; }
        public Decimal EndPrice { get; set; }
        public bool IsActive { get; set; }


    }
}
