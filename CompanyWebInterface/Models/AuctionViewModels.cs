using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using CompanyDataTransferObject;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace CompanyWebInterface.Models;

public class AuctionDisplayViewModel
{
    public Guid Id { get; set; }
    public Guid ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public Guid AuctionManagerId { get; set; }
    public string ManagerName { get; set; } = string.Empty;
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public decimal StartPrice { get; set; }
    public decimal EndPrice { get; set; }
    public decimal CurrentPrice => EndPrice > 0 ? EndPrice : StartPrice;
    public bool IsActive { get; set; }
}

public class AuctionIndexViewModel
{
    public string? Search { get; set; }
    public bool OnlyActive { get; set; }
    public List<AuctionDisplayViewModel> Auctions { get; set; } = new List<AuctionDisplayViewModel>();

    [BindNever]
    [ValidateNever]
    public SelectList Buyers { get; set; } = new SelectList(Array.Empty<UserDTO>());
}

public class AuctionCreateViewModel : IValidatableObject
{
    [Display(Name = "Товар")]
    [Required(ErrorMessage = "Виберіть товар для лоту.")]
    public Guid? ProductId { get; set; }

    [Display(Name = "Стартова ціна (грн)")]
    [Required(ErrorMessage = "Вкажіть стартову ціну.")]
    [Range(0.01, 10000000.0, ErrorMessage = "Стартова ціна має бути від {1} до {2} грн.")]
    public decimal? StartPrice { get; set; }

    [Display(Name = "Дата початку")]
    [Required(ErrorMessage = "Вкажіть дату початку.")]
    public DateTime StartDate { get; set; } = DateTime.Today;

    [Display(Name = "Дата завершення")]
    [Required(ErrorMessage = "Вкажіть дату завершення.")]
    public DateTime EndDate { get; set; } = DateTime.Today.AddDays(7);

    [BindNever]
    [ValidateNever]
    public SelectList Products { get; set; } = new SelectList(Array.Empty<ProductDTO>());

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (EndDate <= StartDate)
        {
            yield return new ValidationResult(
                "Дата завершення має бути пізнішою за дату початку.",
                new[] { nameof(EndDate) });
        }
    }
}

public class BuyoutViewModel
{
    [Required(ErrorMessage = "Не вказано ідентифікатор лоту.")]
    public Guid AuctionId { get; set; }

    [Display(Name = "Покупець")]
    [Required(ErrorMessage = "Виберіть покупця для викупу.")]
    public Guid? BuyerId { get; set; }
}
