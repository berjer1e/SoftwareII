/*
 * Файл: SpecificRoles.cs
 * Призначення: Строго типізовані класи ролей системи з поліморфним перевизначенням прав доступу.
 */

namespace CompanyDataTransferObject.Roles;

/*
 * Клас: ManagerRoleDTO
 * Призначення: Роль менеджера аукціону, що реалізує здатність ICanManageAuctions.
 */
public class ManagerRoleDTO : RoleDTO, ICanManageAuctions
{
    /*
     * Властивість: CanManageAuctions
     * Призначення: Перевизначає базову заборону — менеджер має право керувати аукціонами.
     */
    public override bool CanManageAuctions => true;
}

/*
 * Клас: AdminRoleDTO
 * Призначення: Роль адміністратора системи, що реалізує здатність ICanManageAuctions.
 */
public class AdminRoleDTO : RoleDTO, ICanManageAuctions
{
    /*
     * Властивість: CanManageAuctions
     * Призначення: Перевизначає базову заборону — адміністратор має право керувати аукціонами.
     */
    public override bool CanManageAuctions => true;
}

/*
 * Клас: BuyerRoleDTO
 * Призначення: Роль покупця торгів (за замовчуванням CanManageAuctions = false).
 */
public class BuyerRoleDTO : RoleDTO
{
}

/*
 * Клас: SellerRoleDTO
 * Призначення: Роль продавця товарів (за замовчуванням CanManageAuctions = false).
 */
public class SellerRoleDTO : RoleDTO
{
}
