using System.Collections.Immutable;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using AutoMapper;
using FlashMemo.Model.Domain;
using FlashMemo.Model.Persistence;
using static FlashMemo.Model.Domain.DeckOptions.SchedulingOpt;

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

public class XamlToTextConverter : IValueConverter
{
    public object Convert(object value, Type targetType,
        object parameter, CultureInfo culture)
    {
        if (value is not string xaml || string.IsNullOrWhiteSpace(xaml))
            return "";

        try
        {
            var doc = XamlSerializer.FromXaml(xaml);
            string text = XamlSerializer.GetPlainText(doc);

            int idx = text.IndexOfAny(['\r', '\n']);

            return idx >= 0
                ? text[..idx].Trim()
                : text.Trim();
        }
        
        catch
        {
            return "[invalid]";
        }
    }

    public object ConvertBack(object value, Type targetType,
        object parameter, CultureInfo culture)
        => throw new NotImplementedException();
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

public class ObsColToImmutableArr : IValueConverter<ObservableCollection<int>, ImmutableArray<TimeSpan>>
{
    public ImmutableArray<TimeSpan> Convert(ObservableCollection<int> sourceMember, ResolutionContext context)
    {
        if (sourceMember.Count != LearningStagesCount)
            throw new InvalidOperationException($"There must always be {LearningStagesCount} learning stages");

        return [..sourceMember.Select(x => TimeSpan.FromMinutes(x))];
    }
}

public class ImmutableArrToObsCol : IValueConverter<ImmutableArray<TimeSpan>, ObservableCollection<int>>
{
    public ObservableCollection<int> Convert(ImmutableArray<TimeSpan> sourceMember, ResolutionContext context)
    {
        if (sourceMember.Length != LearningStagesCount)
            throw new InvalidOperationException($"There must always be {LearningStagesCount} learning stages");

        return [..sourceMember.Select(ts => ts.Minutes)];
    }
}
#endregion