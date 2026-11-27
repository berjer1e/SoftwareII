using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace CompanyWebInterface.Models;

public class LoginViewModel
{
    [Display(Name = "Логін")]
    [Required(ErrorMessage = "Введіть логін.")]
    [StringLength(50, MinimumLength = 2, ErrorMessage = "Логін має містити від {2} до {1} символів.")]
    public string Login { get; set; } = string.Empty;

    [Display(Name = "Пароль")]
    [Required(ErrorMessage = "Введіть пароль.")]
    [DataType(DataType.Password)]
    public string Password { get; set; } = string.Empty;

    [Display(Name = "Запам'ятати мене")]
    public bool RememberMe { get; set; }

    public string? ReturnUrl { get; set; }
}

public class RegisterViewModel
{
    [Display(Name = "Логін")]
    [Required(ErrorMessage = "Введіть логін.")]
    [StringLength(50, MinimumLength = 2, ErrorMessage = "Логін має містити від {2} до {1} символів.")]
    public string Login { get; set; } = string.Empty;

    [Display(Name = "Електронна пошта")]
    [EmailAddress(ErrorMessage = "Некоректний формат електронної пошти.")]
    public string? Email { get; set; }

    [Display(Name = "Пароль")]
    [Required(ErrorMessage = "Введіть пароль.")]
    [StringLength(100, MinimumLength = 4, ErrorMessage = "Пароль має містити від {2} до {1} символів.")]
    [DataType(DataType.Password)]
    public string Password { get; set; } = string.Empty;

    [Display(Name = "Повторіть пароль")]
    [Required(ErrorMessage = "Повторіть пароль.")]
    [DataType(DataType.Password)]
    [Compare(nameof(Password), ErrorMessage = "Паролі не співпадають.")]
    public string ConfirmPassword { get; set; } = string.Empty;
}

public class ProfileViewModel
{
    public string Login { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string RoleDisplayName { get; set; } = string.Empty;
    public decimal Balance { get; set; }
    public bool IsManager { get; set; }
    public DateTimeOffset? IssuedUtc { get; set; }
    public DateTimeOffset? ExpiresUtc { get; set; }
    public List<(string Type, string Value)> Claims { get; set; } = new List<(string, string)>();
}
