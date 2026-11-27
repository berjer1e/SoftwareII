using System;
using System.Collections.Generic;
using System.Globalization;
using System.Security.Claims;
using System.Threading.Tasks;
using CompanyDataTransferObject;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Http;

namespace CompanyWebInterface.Security;

public class CookieUserSession : IWebUserSession
{
    private const string RoleIdClaim = "RoleId";
    private const string LoginDateClaim = "LoginDate";
    public static readonly TimeSpan RememberMeDuration = TimeSpan.FromDays(14);

    private readonly IHttpContextAccessor _httpContextAccessor;
    private UserDTO? _overrideUser;
    private DateTime? _overrideLoginDate;

    public CookieUserSession(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public UserDTO? CurrentUser
    {
        get
        {
            if (_overrideUser != null)
            {
                return _overrideUser;
            }

            var principal = _httpContextAccessor.HttpContext?.User;
            if (principal?.Identity == null || !principal.Identity.IsAuthenticated)
            {
                return null;
            }

            var idStr = principal.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!Guid.TryParse(idStr, out Guid userId))
            {
                return null;
            }

            Guid.TryParse(principal.FindFirstValue(RoleIdClaim), out Guid roleId);

            return new UserDTO
            {
                Id = userId,
                Name = principal.Identity.Name ?? string.Empty,
                Email = principal.FindFirstValue(ClaimTypes.Email) ?? string.Empty,
                RoleId = roleId,
                IsActive = true
            };
        }
    }

    public DateTime? LoginDate
    {
        get
        {
            if (_overrideLoginDate.HasValue)
            {
                return _overrideLoginDate;
            }

            var raw = _httpContextAccessor.HttpContext?.User.FindFirstValue(LoginDateClaim);
            return DateTime.TryParse(raw, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var dt)
                ? dt
                : null;
        }
    }

    public bool IsAuthenticated => CurrentUser != null;

    public Task SignInAsync(UserDTO user, bool isPersistent)
    {
        var context = _httpContextAccessor.HttpContext
            ?? throw new InvalidOperationException("HttpContext недоступний.");

        var now = DateTime.Now;
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Name, user.Name),
            new Claim(ClaimTypes.Email, user.Email),
            new Claim(ClaimTypes.Role, user.RoleType.ToString()),
            new Claim(RoleIdClaim, user.RoleId.ToString()),
            new Claim(LoginDateClaim, now.ToString("O", CultureInfo.InvariantCulture))
        };

        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        var properties = new AuthenticationProperties { IsPersistent = isPersistent };
        if (isPersistent)
        {
            properties.ExpiresUtc = DateTimeOffset.UtcNow.Add(RememberMeDuration);
        }

        return context.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            new ClaimsPrincipal(identity),
            properties);
    }

    public Task SignOutAsync()
    {
        var context = _httpContextAccessor.HttpContext
            ?? throw new InvalidOperationException("HttpContext недоступний.");

        _overrideUser = null;
        _overrideLoginDate = null;
        return context.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
    }

    public void StartSession(UserDTO user)
    {
        _overrideUser = user;
        _overrideLoginDate = DateTime.Now;
    }

    public void ClearSession()
    {
        _overrideUser = null;
        _overrideLoginDate = null;
    }
}
