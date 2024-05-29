using System;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Media;

namespace BalanceAct;

public class AmountToBrushConverter : IValueConverter
{
    public object? Convert(object value, Type targetType, object parameter, string language)
    {
        SolidColorBrush result = new(Microsoft.UI.Colors.DodgerBlue);

        if (App.Current.Resources.TryGetValue("SecondaryThemeBrush", out object _))
            result = (Microsoft.UI.Xaml.Media.SolidColorBrush)App.Current.Resources["SecondaryBrush"];

        if (value == null || App.IsClosing)
            return result;

        if (double.TryParse($"{value}", System.Globalization.NumberStyles.Float | System.Globalization.NumberStyles.AllowThousands | System.Globalization.NumberStyles.AllowCurrencySymbol, System.Globalization.CultureInfo.CurrentCulture, out double amnt))
        {
            switch (amnt)
            {
                case double t when t >= 10000d:
                    return new SolidColorBrush(Microsoft.UI.Colors.Red);
                case double t when t >= 5000d:
                    return new SolidColorBrush(Microsoft.UI.Colors.OrangeRed);
                case double t when t >= 1000d:
                    return new SolidColorBrush(Microsoft.UI.Colors.Orange);
                case double t when t >= 500d:
                    return new SolidColorBrush(Microsoft.UI.Colors.Gold);
                case double t when t >= 100d:
                    return new SolidColorBrush(Microsoft.UI.Colors.Yellow);
                default:
                    return result;
            }
        }

        return result;
    }

    public object? ConvertBack(object value, Type targetType, object parameter, string language)
    {
        return null;
    }
}

