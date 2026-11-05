/*
 * Файл: ProductTypeDTO.cs
 * Призначення: Об'єкт передачі даних типу (категорії) товару для класифікації лотів аукціону.
 */

namespace CompanyDataTransferObject
{
    public class ProductTypeDTO : BaseDTO
    {
        public string Name { get; set; } = string.Empty;
    }
}
