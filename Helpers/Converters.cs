using System.Collections.Immutable;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using AutoMapper;
using FlashMemo.Model.Domain;
using FlashMemo.Model.Persistence;

namespace FlashMemo.Helpers;

#region WPF Converters
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

public class TimeSpanHumanizer : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is not ScheduleInfo si)
            return "TYPE ERROR";

        var ts = si.Interval;

        return ts.TotalDays switch
        {
            >= 365 => $"{(int)(ts.TotalDays / 365)} years",
            >= 30 => $"{(int)(ts.TotalDays / 30)} months",
            >= 1 => $"{(int)ts.TotalDays} days",
            _ => $"{(int)ts.TotalMinutes} minutes",
        };
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
#endregion

#region AutoMapper converters
public class DeckListToIds: IValueConverter<List<Deck>, ImmutableArray<long>>
{
    public ImmutableArray<long> Convert(List<Deck> sourceMember, ResolutionContext context)
        => [..sourceMember.Select(d => d.Id)];
}

public class ListToImmutableArr<T> : IValueConverter<List<T>, ImmutableArray<T>>
{
    public ImmutableArray<T> Convert(List<T> sourceMember, ResolutionContext context)
        => [..sourceMember];
}

public class ImmutableArrToList<T> : IValueConverter<ImmutableArray<T>, List<T>>
{
    public List<T> Convert(ImmutableArray<T> sourceMember, ResolutionContext context)
        => [..sourceMember];
}
#endregion