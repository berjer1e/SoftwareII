using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CompanyDataTransferObject
{
    public class ManufacturerDTO: BaseDTO
    {
        
        public string Name { get; set; } = string.Empty;
        public string Country { get; set; } = string.Empty;
    }
}
