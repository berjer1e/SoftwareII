using System;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace CompanyWebInterface.Models;

public class UserBalanceViewModel
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string RoleDisplayName { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public decimal Balance { get; set; }
}

public class UpdateBalanceViewModel
{
    [Required]
    public Guid UserId { get; set; }

    [BindNever]
    [ValidateNever]
    public string UserName { get; set; } = string.Empty;

    [Display(Name = "Новий баланс (грн)")]
    [Required(ErrorMessage = "Введіть суму балансу.")]
    [Range(0.0, 100000000.0, ErrorMessage = "Баланс має бути в межах від {1} до {2} грн.")]
    public decimal? NewBalance { get; set; }
}
