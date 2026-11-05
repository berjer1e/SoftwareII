/*
 * Файл: ProductType.cs
 * Призначення: Сутність бази даних Entity Framework Core для таблиці ProductType (категорії товарів).
 */

using System;
using System.Collections.Generic;

namespace CompanyDataAccessLayer.Entities;

public partial class ProductType
{
    public Guid TypeId { get; set; }

    public string? TypeName { get; set; }

    public virtual ICollection<Product> Products { get; set; } = new List<Product>();
}
