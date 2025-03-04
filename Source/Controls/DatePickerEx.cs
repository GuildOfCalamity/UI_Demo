using System;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml;

namespace UI_Demo;

/// <summary>
///  XAML use example:
/// <example><code>
///  &lt;controls:DatePickerEx Min="2001-01-01T00:00:00Z" Max="2050-12-31T00:00:00Z"/&gt;
/// </code></example></summary>
public partial class DatePickerEx : CalendarDatePicker
{
    public DateTimeOffset Max
    {
        get { return (DateTimeOffset)GetValue(MaxProperty); }
        set { SetValue(MaxProperty, value); }
    }

    public static readonly DependencyProperty MaxProperty = DependencyProperty.Register(
            nameof(Max),
            typeof(DateTimeOffset),
            typeof(DatePickerEx),
            new PropertyMetadata(null, OnMaxChanged));

    static void OnMaxChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var calendar = d as DatePickerEx;
        if (calendar is not null)
            calendar.MaxDate = (DateTimeOffset)e.NewValue;
    }

    public DateTimeOffset Min
    {
        get { return (DateTimeOffset)GetValue(MinProperty); }
        set { SetValue(MinProperty, value); }
    }

    public static readonly DependencyProperty MinProperty = DependencyProperty.Register(
            nameof(Min),
            typeof(DateTimeOffset),
            typeof(DatePickerEx),
            new PropertyMetadata(null, OnMinChanged));

    static void OnMinChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var calendar = d as DatePickerEx;
        if (calendar is not null)
            calendar.MinDate = (DateTimeOffset)e.NewValue;
    }
}
