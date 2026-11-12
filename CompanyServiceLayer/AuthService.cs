/*
 * Файл: AuthService.cs
 * Призначення: Реалізація бізнес-логіки автентифікації користувача, реєстрації, перевірки хешу та сесії.
 */

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

    /*
     * Конструктор: AuthService
     * Призначення: Ініціалізує залежності доступу до даних, поточної сесії та сервісу хешування.
     */
    public AuthService(IUserDAL userDal, IUserSession userSession, IHashService hashService)
    {
        _userDal = userDal;
        _userSession = userSession;
        _hashService = hashService;
    }

    /*
     * Метод: Login
     * Призначення: Знаходить користувача за ім'ям, перевіряє пароль і запускає сесію.
     */
    public bool Login(string username, string password)
    {
        if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
        {
            return false;
        }

        var user = _userDal.GetAll().FirstOrDefault(u => u.Name.Equals(username.Trim(), StringComparison.OrdinalIgnoreCase));
        if (user == null || !user.IsActive)
        {
            return false;
        }

        if (string.IsNullOrWhiteSpace(user.PasswordHash))
        {
            return false;
        }

        byte[]? expectedHash = TryParsePasswordHash(user.PasswordHash);
        if (expectedHash == null)
        {
            return false;
        }

        if (_hashService.VerifyHashedPassword(password, user.Id, expectedHash))
        {
            _userSession.StartSession(user);
            return true;
        }

        return false;
    }

    /*
     * Метод: Register
     * Призначення: Створює нового користувача в системі з роллю менеджера аукціону (Варіант 21).
     */
    public bool Register(string username, string password, string email = "")
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
            RoleId = AppRoles.AuctionManagerRoleId,
            PasswordHash = FormatPasswordHash(newId, hashBytes)
        };

        return _userDal.Add(newUser);
    }

    /*
     * Метод: FormatPasswordHash
     * Призначення: Форматує хеш пароля у вигляд "<16_символів_Id>:<hex_хеш>" для збереження в БД.
     */
    private static string FormatPasswordHash(Guid userId, byte[] hashBytes)
    {
        string prefix = userId.ToString("N")[..16];
        string hexHash = Convert.ToHexString(hashBytes).ToLowerInvariant();
        return $"{prefix}:{hexHash}";
    }

    /*
     * Метод: TryParsePasswordHash
     * Призначення: Витягує масив байтів хешу з рядка формату "<prefix>:<hex>" або "<hex>".
     */
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

    /*
     * Метод: Logout
     * Призначення: Скидає поточну сесію користувача.
     */
    public void Logout()
    {
        _userSession.ClearSession();
    }

    /*
     * Властивість: CanManageAuctions
     * Призначення: Перевіряє права користувача: чи є він Менеджером аукціону або Адміністратором.
     */
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
