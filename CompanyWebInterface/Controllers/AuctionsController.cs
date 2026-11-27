using System;
using System.Collections.Generic;
using System.Linq;
using AutoMapper;
using CompanyDataTransferObject;
using CompanyServiceLayer;
using CompanyWebInterface.Mapping;
using CompanyWebInterface.Models;
using CompanyWebInterface.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace CompanyWebInterface.Controllers;

[Authorize(Roles = WebRoles.Managers)]
public class AuctionsController : Controller
{
    private readonly IAuctionService _auctionService;
    private readonly IMapper _mapper;

    public AuctionsController(IAuctionService auctionService, IMapper mapper)
    {
        _auctionService = auctionService;
        _mapper = mapper;
    }

    [AllowAnonymous]
    [HttpGet]
    public IActionResult Index(string? search = null, bool onlyActive = false)
    {
        var auctions = _auctionService.GetAllAuctions();
        var productNames = GetProductNames();
        var userNames = GetUserNames();

        var mapped = _mapper.Map<List<AuctionDisplayViewModel>>(auctions, opts =>
        {
            opts.Items[DtoToViewModelProfile.ProductNamesKey] = productNames;
            opts.Items[DtoToViewModelProfile.UserNamesKey] = userNames;
        });

        if (onlyActive)
        {
            mapped = mapped.Where(a => a.IsActive).ToList();
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            string term = search.Trim();
            mapped = mapped.Where(a =>
                a.ProductName.Contains(term, StringComparison.OrdinalIgnoreCase) ||
                a.ManagerName.Contains(term, StringComparison.OrdinalIgnoreCase) ||
                a.Id.ToString().Contains(term, StringComparison.OrdinalIgnoreCase))
                .ToList();
        }

        var activeBuyers = _auctionService.GetAllUsers().Where(u => u.IsActive).ToList();

        var model = new AuctionIndexViewModel
        {
            Search = search,
            OnlyActive = onlyActive,
            Auctions = mapped,
            Buyers = new SelectList(activeBuyers, nameof(UserDTO.Id), nameof(UserDTO.Name))
        };

        return View(model);
    }

    [AllowAnonymous]
    [HttpGet]
    public IActionResult Details(Guid id)
    {
        var auction = _auctionService.GetAuctionById(id);
        if (auction == null)
        {
            return NotFound();
        }

        var productNames = GetProductNames();
        var userNames = GetUserNames();

        var model = _mapper.Map<AuctionDisplayViewModel>(auction, opts =>
        {
            opts.Items[DtoToViewModelProfile.ProductNamesKey] = productNames;
            opts.Items[DtoToViewModelProfile.UserNamesKey] = userNames;
        });

        var activeBuyers = _auctionService.GetAllUsers().Where(u => u.IsActive).ToList();
        ViewBag.Buyers = new SelectList(activeBuyers, nameof(UserDTO.Id), nameof(UserDTO.Name));

        return View(model);
    }

    [HttpGet]
    public IActionResult Create()
    {
        var model = new AuctionCreateViewModel();
        FillProducts(model);
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Create(AuctionCreateViewModel model)
    {
        if (ModelState.IsValid)
        {
            var dto = _mapper.Map<AuctionDTO>(model);
            if (_auctionService.AddAuction(dto))
            {
                TempData["StatusMessage"] = "Новий аукціонний лот успішно створено.";
                return RedirectToAction(nameof(Index));
            }

            ModelState.AddModelError(string.Empty, "Не вдалося створити лот (перевірте права менеджера).");
        }

        FillProducts(model);
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Buyout(BuyoutViewModel model)
    {
        if (!ModelState.IsValid || model.BuyerId == null)
        {
            TempData["ErrorMessage"] = "Оберіть покупця для викупу лоту.";
            return RedirectToAction(nameof(Index));
        }

        bool success = _auctionService.BuyoutAuction(model.AuctionId, model.BuyerId.Value, 0.10m);
        if (success)
        {
            TempData["StatusMessage"] = "Лот викуплено (10% комісії нараховано менеджеру).";
        }
        else
        {
            TempData["ErrorMessage"] = "Не вдалося викупити лот: недостатньо коштів у покупця або лот уже закритий.";
        }

        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Close(Guid id)
    {
        if (_auctionService.CloseAuction(id))
        {
            TempData["StatusMessage"] = "Аукціон завершено менеджером.";
        }
        else
        {
            TempData["ErrorMessage"] = "Не вдалося завершити аукціон.";
        }

        return RedirectToAction(nameof(Details), new { id });
    }

    [Authorize]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult PlaceBid(Guid id, decimal? bidAmount)
    {
        if (bidAmount == null || bidAmount <= 0)
        {
            TempData["ErrorMessage"] = "Вкажіть коректну суму ставки.";
            return RedirectToAction(nameof(Details), new { id });
        }

        if (_auctionService.PlaceBid(id, bidAmount.Value))
        {
            TempData["StatusMessage"] = $"Ставку {bidAmount.Value:N2} грн прийнято.";
        }
        else
        {
            TempData["ErrorMessage"] = "Ставку відхилено (ставка має перевищувати поточну ціну, а менеджери не можуть робити ставки).";
        }

        return RedirectToAction(nameof(Details), new { id });
    }

    private void FillProducts(AuctionCreateViewModel model)
    {
        var products = _auctionService.GetAllProducts();
        model.Products = new SelectList(products, nameof(ProductDTO.Id), nameof(ProductDTO.Name), model.ProductId);
    }

    private IReadOnlyDictionary<Guid, string> GetProductNames()
    {
        return _auctionService.GetAllProducts()
            .GroupBy(p => p.Id)
            .ToDictionary(g => g.Key, g => g.First().Name);
    }

    private IReadOnlyDictionary<Guid, string> GetUserNames()
    {
        return _auctionService.GetAllUsers()
            .GroupBy(u => u.Id)
            .ToDictionary(g => g.Key, g => g.First().Name);
    }
}
