/*
 * Файл: BooleanConverters.cs
 * Призначення: Конвертери значень IValueConverter для перетворення логічних властивостей (bool)
 * у значення видимості елементів WPF (Visibility) та інвертовані логічні значення.
 */

using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace CompanyWPFInterface.Infrastructure
{
    public class InvertableBooleanToVisibilityConverter : IValueConverter
    {
        /*
         * Перетворює значення типу bool у Visibility.Visible або Visibility.Collapsed (з підтримкою параметра Inverse).
         */
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            bool flag = value is bool b && b;
            if (parameter is string param && string.Equals(param, "Inverse", StringComparison.OrdinalIgnoreCase))
            {
                flag = !flag;
            }

            return flag ? Visibility.Visible : Visibility.Collapsed;
        }

        /*
         * Виконує зворотне перетворення значення Visibility у тип bool.
         */
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            bool isVisible = value is Visibility v && v == Visibility.Visible;
            if (parameter is string param && string.Equals(param, "Inverse", StringComparison.OrdinalIgnoreCase))
            {
                return !isVisible;
            }

            return isVisible;
        }
    }
}
