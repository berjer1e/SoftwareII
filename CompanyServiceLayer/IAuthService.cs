using System;
using CompanyDataTransferObject;

namespace CompanyServiceLayer;

public interface IAuthService
{
    UserDTO? ValidateCredentials(string username, string password);
    bool Login(string username, string password);
    bool Register(string username, string password, string email = "", Guid? roleId = null);
    void Logout();
    bool CanManageAuctions { get; }
}
