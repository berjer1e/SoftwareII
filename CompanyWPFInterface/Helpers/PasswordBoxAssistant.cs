/*
 * Файл: PasswordBoxAssistant.cs
 * Призначення: Attached-властивість для безпечного двостороннього зв'язування PasswordBox у патерні MVVM.
 */

using System.Windows;
using System.Windows.Controls;

namespace CompanyWPFInterface.Helpers;

public static class PasswordBoxAssistant
{
    /*
     * Властивість: BoundPasswordProperty
     * Призначення: Зберігає прив'язане значення пароля для PasswordBox.
     */
    public static readonly DependencyProperty BoundPasswordProperty =
        DependencyProperty.RegisterAttached(
            "BoundPassword",
            typeof(string),
            typeof(PasswordBoxAssistant),
            new FrameworkPropertyMetadata(string.Empty, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnBoundPasswordChanged));

    /*
     * Властивість: BindPasswordProperty
     * Призначення: Активує відстеження події PasswordChanged на PasswordBox.
     */
    public static readonly DependencyProperty BindPasswordProperty =
        DependencyProperty.RegisterAttached(
            "BindPassword",
            typeof(bool),
            typeof(PasswordBoxAssistant),
            new PropertyMetadata(false, OnBindPasswordChanged));

    private static readonly DependencyProperty UpdatingPasswordProperty =
        DependencyProperty.RegisterAttached("UpdatingPassword", typeof(bool), typeof(PasswordBoxAssistant), new PropertyMetadata(false));

    /*
     * Метод: GetBoundPassword
     * Призначення: Повертає прив'язаний пароль із залежної властивості.
     */
    public static string GetBoundPassword(DependencyObject dp) => (string)dp.GetValue(BoundPasswordProperty);

    /*
     * Метод: SetBoundPassword
     * Призначення: Записує пароль у залежну властивість.
     */
    public static void SetBoundPassword(DependencyObject dp, string value) => dp.SetValue(BoundPasswordProperty, value);

    /*
     * Метод: GetBindPassword
     * Призначення: Повертає прапорець активації прив'язки пароля.
     */
    public static bool GetBindPassword(DependencyObject dp) => (bool)dp.GetValue(BindPasswordProperty);

    /*
     * Метод: SetBindPassword
     * Призначення: Встановлює прапорець активації прив'язки пароля.
     */
    public static void SetBindPassword(DependencyObject dp, bool value) => dp.SetValue(BindPasswordProperty, value);

    private static void OnBoundPasswordChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not PasswordBox box || (bool)box.GetValue(UpdatingPasswordProperty))
        {
            return;
        }

        string newValue = (string?)e.NewValue ?? string.Empty;
        if (box.Password != newValue)
        {
            box.Password = newValue;
        }
    }

    private static void OnBindPasswordChanged(DependencyObject dp, DependencyPropertyChangedEventArgs e)
    {
        if (dp is not PasswordBox box)
        {
            return;
        }

        if ((bool)e.OldValue)
        {
            box.PasswordChanged -= HandlePasswordChanged;
        }

        if ((bool)e.NewValue)
        {
            box.PasswordChanged += HandlePasswordChanged;
        }
    }

    private static void HandlePasswordChanged(object sender, RoutedEventArgs e)
    {
        if (sender is not PasswordBox box)
        {
            return;
        }

        box.SetValue(UpdatingPasswordProperty, true);
        SetBoundPassword(box, box.Password);
        box.SetValue(UpdatingPasswordProperty, false);
    }
}
