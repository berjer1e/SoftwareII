using System.Threading.Tasks;
using CompanyDataTransferObject;
using CompanyServiceLayer;

namespace CompanyWebInterface.Security;

public interface IWebUserSession : IUserSession
{
    Task SignInAsync(UserDTO user, bool isPersistent);
    Task SignOutAsync();
}
