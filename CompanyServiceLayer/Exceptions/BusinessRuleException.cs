/*
 * Файл: BusinessRuleException.cs
 * Призначення: Спеціалізований клас винятку рівня бізнес-логіки для передачі однієї або декількох
 * помилок порушення бізнес-правил, валідації або прав доступу до інтерфейсу користувача.
 */

using System;
using System.Collections.Generic;
using System.Linq;

namespace CompanyServiceLayer.Exceptions
{
    public class BusinessRuleException : Exception
    {
        public IReadOnlyList<string> Errors { get; }

        /*
         * Ініціалізує виняток бізнес-логіки з одним повідомленням про помилку.
         */
        public BusinessRuleException(string message)
            : base(message)
        {
            Errors = new List<string> { message };
        }

        /*
         * Ініціалізує виняток бізнес-логіки з переліком помилок валідації, об'єднуючи їх у загальний текст винятку.
         */
        public BusinessRuleException(IEnumerable<string> errors)
            : base(string.Join(" ", errors))
        {
            var list = errors?.Where(e => !string.IsNullOrWhiteSpace(e)).ToList() ?? new List<string>();
            if (list.Count == 0)
            {
                list.Add("Порушено бізнес-правило.");
            }
            Errors = list;
        }
    }
}
