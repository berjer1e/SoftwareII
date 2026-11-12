/*
 * Файл: RoleDTO.cs
 * Призначення: Базова модель передачі даних ролі користувача з поліморфною перевіркою прав доступу.
 */

namespace CompanyDataTransferObject;

public class RoleDTO : BaseDTO
{
    /*
     * Властивість: Name
     * Призначення: Назва ролі користувача.
     */
    public string Name { get; set; } = string.Empty;

    /*
     * Властивість: CanManageAuctions
     * Призначення: Поліморфна ознака права керувати аукціонами (за замовчуванням заборонено).
     */
    public virtual bool CanManageAuctions => false;
}
