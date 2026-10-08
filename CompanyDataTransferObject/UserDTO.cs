using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CompanyDataTransferObject
{
    public class UserDTO: BaseDTO
    {

        public string Name { get; set; } = string.Empty;
      
        public string Email { get; set; } = string.Empty;
        public DateOnly RegDate { get; set; }= DateOnly.FromDateTime(DateTime.Now);
        public bool IsActive { get; set; } = true;
        public Guid RoleId { get; set; }
    }
}
