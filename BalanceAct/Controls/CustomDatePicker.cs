using System;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml;

namespace BalanceAct.Controls;

/// <summary>
/// <example><code>
/// &lt;controls:CustomCalendarDatePicker Min="2001-01-01T00:00:00Z" Max="2050-01-01T00:00:00Z"/&gt;
/// </code></example>
/// </summary>
public class CustomDatePicker : CalendarDatePicker
{
    public DateTimeOffset Max
    {
        get { return (DateTimeOffset)GetValue(MaxProperty); }
        set { SetValue(MaxProperty, value); }
    }

    public static readonly DependencyProperty MaxProperty = DependencyProperty.Register(
            nameof(Max),
            typeof(DateTimeOffset),
            typeof(CustomDatePicker),
            new PropertyMetadata(null, OnMaxChanged));

    static void OnMaxChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var calendar = d as CustomDatePicker;
        if (calendar is not null)
            calendar.MaxDate = (DateTimeOffset)e.NewValue;
    }

    public DateTimeOffset Min
    {
        get { return (DateTimeOffset)GetValue(MinProperty); }
        set { SetValue(MinProperty, value); }
    }

    public static readonly DependencyProperty MinProperty =  DependencyProperty.Register(
            nameof(Min),
            typeof(DateTimeOffset),
            typeof(CustomDatePicker),
            new PropertyMetadata(null, OnMinChanged));

    static void OnMinChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var calendar = d as CustomDatePicker;
        if (calendar is not null)
            calendar.MinDate = (DateTimeOffset)e.NewValue;
    }
}
