/*
 * Файл: BaseDTO.cs
 * Призначення: Абстрактний базовий клас для всіх об'єктів передачі даних (DTO) системи,
 * що визначає універсальний унікальний ідентифікатор запису.
 */

using System;

namespace CompanyDataTransferObject
{
    public abstract class BaseDTO
    {
        public Guid Id { get; set; }
    }
}
