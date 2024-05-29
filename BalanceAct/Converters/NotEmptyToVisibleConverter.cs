using System;

using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml;

namespace BalanceAct;

public class NotEmptyToVisibleConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        if (value is string str)
        {
            return string.IsNullOrEmpty(str) is false
                ? Visibility.Visible
                : Visibility.Collapsed;
        }

        throw new ArgumentException("NotEmptyToVisibleConverter: Value must be a 'string'.");
    }

    public object? ConvertBack(object value, Type targetType, object parameter, string language)
    {
        return null;
    }
}
