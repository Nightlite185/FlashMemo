using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace FlashMemo.Helpers;

public class NullToVisConverter: IValueConverter
{
    public object Convert(object? value, Type targetType, object parameter, CultureInfo culture)
        => value == null
            ? Visibility.Collapsed
            : Visibility.Visible;

    public object ConvertBack(object value, Type targetType,object parameter, CultureInfo culture)
        => throw new NotImplementedException();
}

public class BoolToVisConverter : IValueConverter
{
    public object Convert(object value, Type t, object p, CultureInfo c)
        => (bool)value ? Visibility.Visible : Visibility.Collapsed;

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotImplementedException();
}

public class InverseBoolConverter : IValueConverter
{
    public object Convert(object value, Type t, object p, CultureInfo c)
        => !(bool)value;

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotImplementedException();
}

public class InverseBoolToVisConverter : IValueConverter
{
    public object Convert(object value, Type t, object p, CultureInfo c)
        => (bool)value ? Visibility.Collapsed : Visibility.Visible;

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotImplementedException();
}