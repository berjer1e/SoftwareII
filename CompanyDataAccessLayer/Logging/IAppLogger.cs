/*
 * Файл: IAppLogger.cs
 * Призначення: Інтерфейс сервісу журналювання подій, попереджень та помилок роботи програми.
 */

using System;

namespace CompanyDataAccessLayer.Logging
{
    public interface IAppLogger
    {
        /*
         * Записує інформаційне повідомлення про штатну подію в журнал роботи програми.
         */
        void LogInfo(string message);

        /*
         * Записує попередження про потенційно небезпечну ситуацію в журнал роботи програми.
         */
        void LogWarning(string message);

        /*
         * Записує деталі винятку та контекстне повідомлення про помилку в журнал роботи програми.
         */
        void LogError(Exception exception, string contextMessage);
    }
}
