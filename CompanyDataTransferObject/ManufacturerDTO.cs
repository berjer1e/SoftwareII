/*
 * Файл: ManufacturerDTO.cs
 * Призначення: Об'єкт передачі даних виробника товарів із зазначенням назви компанії та країни походження.
 */

namespace CompanyDataTransferObject
{
    public class ManufacturerDTO : BaseDTO
    {
        public string Name { get; set; } = string.Empty;
        public string Country { get; set; } = string.Empty;
    }
}
