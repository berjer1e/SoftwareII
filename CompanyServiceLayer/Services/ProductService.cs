/*
 * Файл: ProductService.cs
 * Призначення: Реалізація сервісу бізнес-логіки для керування товарами.
 * Перевіряє коректність назви, базової ціни, кількості, категорії та виробника товару.
 */

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CompanyDataAccessLayer.DAL;
using CompanyDataAccessLayer.Logging;
using CompanyDataTransferObject;
using CompanyServiceLayer.Exceptions;
using CompanyServiceLayer.Session;

namespace CompanyServiceLayer.Services
{
    public class ProductService : IProductService
    {
        private readonly IProductDAL _productDal;
        private readonly IUserSession _userSession;
        private readonly IAppLogger _logger;

        /*
         * Ініціалізує сервіс товарів залежностями ProductDAL, сесії користувача та логера.
         */
        public ProductService(
            IProductDAL productDal,
            IUserSession userSession,
            IAppLogger? logger = null)
        {
            _productDal = productDal ?? throw new ArgumentNullException(nameof(productDal));
            _userSession = userSession ?? throw new ArgumentNullException(nameof(userSession));
            _logger = logger ?? FileAppLogger.Instance;
        }

        /*
         * Повертає всі товари з бази даних.
         */
        public List<ProductDTO> GetAll() => _productDal.GetAll();

        /*
         * Асинхронно завантажує всі товари з бази даних.
         */
        public Task<List<ProductDTO>> GetAllAsync() => Task.Run(GetAll);

        /*
         * Повертає товар за його унікальним ідентифікатором.
         */
        public ProductDTO? GetById(Guid id) => _productDal.GetById(id);

        /*
         * Виконує пошук і сортування товарів через рівень DAL.
         */
        public List<ProductDTO> SearchAndSort(string? searchTerm, string? sortBy, bool ascending = true)
        {
            return _productDal.SearchAndSort(searchTerm, sortBy, ascending);
        }

        /*
         * Асинхронно виконує пошук і сортування товарів у фоновому потоці.
         */
        public Task<List<ProductDTO>> SearchAndSortAsync(string? searchTerm, string? sortBy, bool ascending = true)
        {
            return Task.Run(() => SearchAndSort(searchTerm, sortBy, ascending));
        }

        /*
         * Додає новий товар до каталогу після перевірки авторизації та валідації всіх полів товару.
         */
        public ProductDTO CreateProduct(ProductDTO product)
        {
            if (product == null) throw new ArgumentNullException(nameof(product));
            if (!_userSession.IsAuthenticated)
            {
                throw new BusinessRuleException("Для створення товару необхідно увійти в систему.");
            }

            ValidateProduct(product);

            if (!_productDal.Add(product))
            {
                throw new BusinessRuleException("Не вдалося зберегти новий товар у базі даних.");
            }

            _logger.LogInfo($"User '{_userSession.CurrentUser!.Name}' created product '{product.Name}' ({product.Id})");
            return _productDal.GetById(product.Id) ?? product;
        }

        /*
         * Оновлює існуючий товар після перевірки прав ролі Менеджера аукціонів та валідації полів.
         */
        public ProductDTO UpdateProduct(ProductDTO product)
        {
            if (product == null) throw new ArgumentNullException(nameof(product));
            EnsureManagerAccess();

            if (_productDal.GetById(product.Id) == null)
            {
                throw new BusinessRuleException("Товар для редагування не знайдено.");
            }

            ValidateProduct(product);

            if (!_productDal.Update(product))
            {
                throw new BusinessRuleException("Не вдалося оновити товар у базі даних.");
            }

            _logger.LogInfo($"Manager '{_userSession.CurrentUser!.Name}' updated product '{product.Name}' ({product.Id})");
            return _productDal.GetById(product.Id) ?? product;
        }

        /*
         * Видаляє товар із бази даних за умови наявності прав менеджера та відсутності пов'язаних обмежень.
         */
        public bool DeleteProduct(Guid id)
        {
            EnsureManagerAccess();

            if (_productDal.GetById(id) == null)
            {
                throw new BusinessRuleException("Товар для видалення не знайдено.");
            }

            if (!_productDal.Delete(id))
            {
                throw new BusinessRuleException("Неможливо видалити товар, оскільки він використовується в аукціонах.");
            }

            _logger.LogInfo($"Manager '{_userSession.CurrentUser!.Name}' deleted product {id}");
            return true;
        }

        /*
         * Перевіряє валідність полів товару (назва, базова ціна, кількість, категорія та виробник).
         */
        private static void ValidateProduct(ProductDTO product)
        {
            var errors = new List<string>();

            if (string.IsNullOrWhiteSpace(product.Name))
            {
                errors.Add("Назва товару не може бути порожньою.");
            }
            else if (product.Name.Trim().Length > 50)
            {
                errors.Add("Назва товару не може перевищувати 50 символів.");
            }

            if (product.Price <= 0m)
            {
                errors.Add("Базова ціна товару повинна бути більшою за 0.");
            }

            if (product.Quantity <= 0)
            {
                errors.Add("Кількість одиниць товару повинна бути більшою за 0.");
            }

            if (product.ProductTypeId == Guid.Empty)
            {
                errors.Add("Необхідно обрати категорію (тип) товару.");
            }

            if (product.ManufacturerId == Guid.Empty)
            {
                errors.Add("Необхідно обрати виробника товару.");
            }

            if (errors.Count > 0)
            {
                throw new BusinessRuleException(errors);
            }

            product.Name = product.Name.Trim();
        }

        /*
         * Перевіряє наявність у поточного користувача прав менеджера аукціонів або адміністратора.
         */
        private void EnsureManagerAccess()
        {
            if (!_userSession.IsAuthenticated)
            {
                throw new BusinessRuleException("Для виконання цієї дії необхідно увійти в систему.");
            }
            if (!_userSession.CanManageAuctions)
            {
                throw new BusinessRuleException("Доступ заборонено: керування каталогом товарів доступне лише Менеджеру аукціонів.");
            }
        }
    }
}
