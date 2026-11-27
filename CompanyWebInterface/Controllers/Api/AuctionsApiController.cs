using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using AutoMapper;
using CompanyServiceLayer;
using CompanyWebInterface.Mapping;
using CompanyWebInterface.Models;
using Microsoft.AspNetCore.Mvc;

namespace CompanyWebInterface.Controllers.Api;

[ApiController]
[Route("api/auctions")]
public class AuctionsApiController : ControllerBase
{
    private readonly IAuctionService _auctionService;
    private readonly IMapper _mapper;

    public AuctionsApiController(IAuctionService auctionService, IMapper mapper)
    {
        _auctionService = auctionService;
        _mapper = mapper;
    }

    [HttpGet]
    public ActionResult<List<AuctionDisplayViewModel>> GetAll()
    {
        var auctions = _auctionService.GetAllAuctions();
        var products = _auctionService.GetAllProducts()
            .GroupBy(p => p.Id)
            .ToDictionary(g => g.Key, g => g.First().Name);
        var users = _auctionService.GetAllUsers()
            .GroupBy(u => u.Id)
            .ToDictionary(g => g.Key, g => g.First().Name);

        var result = _mapper.Map<List<AuctionDisplayViewModel>>(auctions, opts =>
        {
            opts.Items[DtoToViewModelProfile.ProductNamesKey] = products;
            opts.Items[DtoToViewModelProfile.UserNamesKey] = users;
        });

        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    public ActionResult<AuctionDisplayViewModel> GetById(Guid id)
    {
        var auction = _auctionService.GetAuctionById(id);
        if (auction == null)
        {
            return NotFound(new { message = "Аукціон не знайдено." });
        }

        var products = _auctionService.GetAllProducts()
            .GroupBy(p => p.Id)
            .ToDictionary(g => g.Key, g => g.First().Name);
        var users = _auctionService.GetAllUsers()
            .GroupBy(u => u.Id)
            .ToDictionary(g => g.Key, g => g.First().Name);

        var result = _mapper.Map<AuctionDisplayViewModel>(auction, opts =>
        {
            opts.Items[DtoToViewModelProfile.ProductNamesKey] = products;
            opts.Items[DtoToViewModelProfile.UserNamesKey] = users;
        });

        return Ok(result);
    }

    [HttpGet("users")]
    public ActionResult<List<UserBalanceViewModel>> GetUsers()
    {
        var users = _auctionService.GetAllUsers();
        return Ok(_mapper.Map<List<UserBalanceViewModel>>(users));
    }

    [HttpPost("{id:guid}/buyout")]
    public IActionResult Buyout(Guid id, [FromBody] BuyoutApiRequest request)
    {
        if (_auctionService.GetAuctionById(id) == null)
        {
            return NotFound(new { message = "Аукціон не знайдено." });
        }

        bool success = _auctionService.BuyoutAuction(id, request.BuyerId, 0.10m);
        if (!success)
        {
            return BadRequest(new { message = "Не вдалося викупити лот (недостатньо коштів або лот неактивний)." });
        }

        return Ok(new { message = "Лот викуплено з 10% комісією." });
    }

    [HttpPut("users/{id:guid}/balance")]
    public IActionResult UpdateBalance(Guid id, [FromBody] UpdateBalanceApiRequest request)
    {
        if (_auctionService.GetUserById(id) == null)
        {
            return NotFound(new { message = "Користувача не знайдено." });
        }

        if (!_auctionService.UpdateBalance(id, request.NewBalance))
        {
            return BadRequest(new { message = "Не вдалося оновити баланс." });
        }

        return Ok(new { message = "Баланс успішно оновлено.", userId = id, newBalance = request.NewBalance });
    }

    public class BuyoutApiRequest
    {
        [Required]
        public Guid BuyerId { get; set; }
    }

    public class UpdateBalanceApiRequest
    {
        [Range(0.0, 100000000.0)]
        public decimal NewBalance { get; set; }
    }
}
