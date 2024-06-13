using System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Data;

namespace BalanceAct;

public enum DelayTime
{
    None = 0,
    Short = 1,
    Medium = 2,
    Long = 4
}

public class DelayEnumToBooleanConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        if (parameter != null && parameter is string enumString)
        {
            if (!Enum.IsDefined(typeof(DelayTime), value))
            {
                throw new ArgumentException("EnumToBooleanConverter: value must be an enum");
            }
            var enumValue = Enum.Parse(typeof(DelayTime), enumString);
            return enumValue.Equals(value);
        }
        System.Diagnostics.Debug.WriteLine($"'{nameof(parameter)}' was empty, nothing to do.");
        return false;
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        if (parameter is string enumString)
        {
            return Enum.Parse(typeof(ElementTheme), enumString);
        }
        throw new ArgumentException("EnumToBooleanConverter: parameter must be an enum name");
    }
}