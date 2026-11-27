using System;
using System.Collections.Generic;
using AutoMapper;
using CompanyServiceLayer;
using CompanyWebInterface.Models;
using CompanyWebInterface.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CompanyWebInterface.Controllers;

[Authorize(Roles = WebRoles.Managers)]
public class UsersController : Controller
{
    private readonly IAuctionService _auctionService;
    private readonly IMapper _mapper;

    public UsersController(IAuctionService auctionService, IMapper mapper)
    {
        _auctionService = auctionService;
        _mapper = mapper;
    }

    [HttpGet]
    public IActionResult Index()
    {
        var users = _auctionService.GetAllUsers();
        var model = _mapper.Map<List<UserBalanceViewModel>>(users);
        return View(model);
    }

    [HttpGet]
    public IActionResult EditBalance(Guid id)
    {
        var user = _auctionService.GetUserById(id);
        if (user == null)
        {
            return NotFound();
        }

        var model = new UpdateBalanceViewModel
        {
            UserId = user.Id,
            UserName = user.Name,
            NewBalance = user.Balance
        };

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult EditBalance(Guid id, UpdateBalanceViewModel model)
    {
        var user = _auctionService.GetUserById(id);
        if (user == null)
        {
            return NotFound();
        }

        model.UserId = id;
        model.UserName = user.Name;

        if (!ModelState.IsValid || model.NewBalance == null)
        {
            return View(model);
        }

        if (_auctionService.UpdateBalance(id, model.NewBalance.Value))
        {
            TempData["StatusMessage"] = $"Баланс користувача «{user.Name}» оновлено до {model.NewBalance.Value:N2} грн.";
            return RedirectToAction(nameof(Index));
        }

        ModelState.AddModelError(string.Empty, "Не вдалося оновити баланс користувача.");
        return View(model);
    }
}
