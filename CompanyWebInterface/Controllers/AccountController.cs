using System.Linq;
using System.Threading.Tasks;
using CompanyDataTransferObject.Constants;
using CompanyServiceLayer;
using CompanyWebInterface.Models;
using CompanyWebInterface.Security;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CompanyWebInterface.Controllers;

public class AccountController : Controller
{
    private readonly IAuthService _authService;
    private readonly IAuctionService _auctionService;
    private readonly IWebUserSession _userSession;

    public AccountController(IAuthService authService, IAuctionService auctionService, IWebUserSession userSession)
    {
        _authService = authService;
        _auctionService = auctionService;
        _userSession = userSession;
    }

    [HttpGet]
    public IActionResult Login(string? returnUrl = null)
    {
        return View(new LoginViewModel { ReturnUrl = returnUrl });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var user = _authService.ValidateCredentials(model.Login, model.Password);
        if (user == null)
        {
            ModelState.AddModelError(string.Empty, "Невірний логін або пароль (або акаунт неактивний).");
            return View(model);
        }

        await _userSession.SignInAsync(user, model.RememberMe);
        TempData["StatusMessage"] = $"Вітаємо, {user.Name}! Ваша роль: {AppRoles.GetDisplayName(user.RoleType)}.";
        return RedirectToLocal(model.ReturnUrl);
    }

    [HttpGet]
    public IActionResult Register()
    {
        return View(new RegisterViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Register(RegisterViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        bool created = _authService.Register(
            model.Login,
            model.Password,
            model.Email ?? string.Empty,
            AppRoles.BuyerRoleId);

        if (!created)
        {
            ModelState.AddModelError(string.Empty, "Користувач із таким логіном уже існує.");
            return View(model);
        }

        var user = _authService.ValidateCredentials(model.Login, model.Password);
        if (user != null)
        {
            await _userSession.SignInAsync(user, isPersistent: false);
        }

        TempData["StatusMessage"] = $"Обліковий запис «{model.Login.Trim()}» створено (роль: Покупець).";
        return RedirectToAction(nameof(AuctionsController.Index), "Auctions");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await _userSession.SignOutAsync();
        TempData["StatusMessage"] = "Ви вийшли з системи.";
        return RedirectToAction(nameof(AuctionsController.Index), "Auctions");
    }

    [Authorize]
    [HttpGet]
    public async Task<IActionResult> Profile()
    {
        var currentUser = _userSession.CurrentUser;
        if (currentUser == null)
        {
            return Challenge();
        }

        var dbUser = _auctionService.GetUserById(currentUser.Id) ?? currentUser;
        var authResult = HttpContext != null ? await HttpContext.AuthenticateAsync() : null;

        var model = new ProfileViewModel
        {
            Login = dbUser.Name,
            Email = dbUser.Email,
            RoleDisplayName = AppRoles.GetDisplayName(dbUser.RoleType),
            Balance = dbUser.Balance,
            IsManager = dbUser.IsManager,
            IssuedUtc = authResult?.Properties?.IssuedUtc,
            ExpiresUtc = authResult?.Properties?.ExpiresUtc,
            Claims = User.Claims.Select(c => (c.Type, c.Value)).ToList()
        };

        return View(model);
    }

    [HttpGet]
    public IActionResult AccessDenied()
    {
        return View();
    }

    private IActionResult RedirectToLocal(string? returnUrl)
    {
        if (!string.IsNullOrEmpty(returnUrl) && Url != null && Url.IsLocalUrl(returnUrl))
        {
            return Redirect(returnUrl);
        }

        return RedirectToAction(nameof(AuctionsController.Index), "Auctions");
    }
}
