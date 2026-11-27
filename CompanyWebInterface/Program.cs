using System;
using System.Globalization;
using CompanyDataAccessLayer.DAL;
using CompanyServiceLayer;
using CompanyServiceLayer.Session;
using CompanyWebInterface.Mapping;
using CompanyWebInterface.Security;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Localization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var builder = WebApplication.CreateBuilder(args);

string connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Рядок підключення 'DefaultConnection' не знайдено в appsettings.json.");

builder.Services.AddControllersWithViews(options =>
{
    var messages = options.ModelBindingMessageProvider;
    messages.SetValueMustBeANumberAccessor(field => $"Поле «{field}» має бути числом.");
    messages.SetAttemptedValueIsInvalidAccessor((value, field) => $"Значення «{value}» не підходить для поля «{field}».");
    messages.SetValueMustNotBeNullAccessor(_ => "Поле має бути заповнене.");
});

builder.Services.AddAutoMapper(cfg => cfg.AddProfile<DtoToViewModelProfile>());

builder.Services.AddScoped<IUserDAL>(_ => new UserDAL(connectionString));
builder.Services.AddScoped<IAuctionDAL>(_ => new AuctionDAL(connectionString));
builder.Services.AddScoped<IProductDAL>(_ => new ProductDAL(connectionString));

builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<IWebUserSession, CookieUserSession>();
builder.Services.AddScoped<IUserSession>(sp => sp.GetRequiredService<IWebUserSession>());

builder.Services.AddSingleton<IHashService, HashService>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IAuctionService, AuctionService>();

builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Account/Login";
        options.LogoutPath = "/Account/Logout";
        options.AccessDeniedPath = "/Account/AccessDenied";

        options.Cookie.Name = "CompanyAuction.Auth";
        options.Cookie.HttpOnly = true;
        options.Cookie.SameSite = SameSiteMode.Lax;
        options.ExpireTimeSpan = TimeSpan.FromMinutes(30);
        options.SlidingExpiration = true;
    });

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseRequestLocalization(new RequestLocalizationOptions
{
    DefaultRequestCulture = new RequestCulture(CultureInfo.InvariantCulture),
    SupportedCultures = new[] { CultureInfo.InvariantCulture },
    SupportedUICultures = new[] { CultureInfo.InvariantCulture }
});

app.UseHttpsRedirection();
app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Auctions}/{action=Index}/{id?}")
    .WithStaticAssets();

app.Run();
