using CompanyDataAccessLayer.DAL;
using CompanyDataTransferObject;
using CompanyDataTransferObject.Constants;
using CompanyDataTransferObject.Enums;

namespace CompanyServiceLayer;

public class AuthService : IAuthService
{
    private readonly IUserDAL _userDal;
    private readonly IUserSession _userSession;
    private readonly IHashService _hashService;

    public AuthService(IUserDAL userDal, IUserSession userSession, IHashService hashService)
    {
        _userDal = userDal;
        _userSession = userSession;
        _hashService = hashService;
    }

    public UserDTO? ValidateCredentials(string username, string password)
    {
        if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
        {
            return null;
        }

        var user = _userDal.GetAll().FirstOrDefault(u => u.Name.Equals(username.Trim(), StringComparison.OrdinalIgnoreCase));
        if (user == null || !user.IsActive || string.IsNullOrWhiteSpace(user.PasswordHash))
        {
            return null;
        }

        byte[]? expectedHash = TryParsePasswordHash(user.PasswordHash);
        if (expectedHash == null)
        {
            return null;
        }

        return _hashService.VerifyHashedPassword(password, user.Id, expectedHash) ? user : null;
    }

    public bool Login(string username, string password)
    {
        var user = ValidateCredentials(username, password);
        if (user == null)
        {
            return false;
        }

        _userSession.StartSession(user);
        return true;
    }

    public bool Register(string username, string password, string email = "", Guid? roleId = null)
    {
        if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
        {
            return false;
        }

        var existing = _userDal.GetAll().FirstOrDefault(u => u.Name.Equals(username.Trim(), StringComparison.OrdinalIgnoreCase));
        if (existing != null)
        {
            return false;
        }

        Guid newId = Guid.NewGuid();
        byte[] hashBytes = _hashService.HashPassword(password, newId);

        var newUser = new UserDTO
        {
            Id = newId,
            Name = username.Trim(),
            Email = string.IsNullOrWhiteSpace(email) ? $"{username.Trim()}@auction.com" : email.Trim(),
            RegDate = DateOnly.FromDateTime(DateTime.Now),
            IsActive = true,
            RoleId = roleId ?? AppRoles.AuctionManagerRoleId,
            PasswordHash = FormatPasswordHash(newId, hashBytes)
        };

        return _userDal.Add(newUser);
    }

    private static string FormatPasswordHash(Guid userId, byte[] hashBytes)
    {
        string prefix = userId.ToString("N")[..16];
        string hexHash = Convert.ToHexString(hashBytes).ToLowerInvariant();
        return $"{prefix}:{hexHash}";
    }

    private static byte[]? TryParsePasswordHash(string storedHash)
    {
        string hexPart = storedHash.Contains(':')
            ? storedHash.Split(':', 2)[1]
            : storedHash;

        try
        {
            return Convert.FromHexString(hexPart.Trim());
        }
        catch (FormatException)
        {
            return null;
        }
    }

    public void Logout()
    {
        _userSession.ClearSession();
    }

    public bool CanManageAuctions
    {
        get
        {
            var user = _userSession.CurrentUser;
            if (user == null)
            {
                return false;
            }

            return user.RoleType == UserRoleType.AuctionManager || user.RoleType == UserRoleType.Admin;
        }
    }
}
