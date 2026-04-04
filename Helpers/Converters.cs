using System.Collections.Immutable;
using System.Globalization;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;
using AutoMapper;
using FlashMemo.Model.Domain;
using FlashMemo.Model.Persistence;
using FlashMemo.ViewModel.Wrappers;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

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
        => !(bool)value;
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
            return XamlSerializer
                .GetPlainText(xaml)
                .Trim();
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

public partial class EnumToReadableTextConverter : IValueConverter
{
    [GeneratedRegex("([a-z0-9])([A-Z])", RegexOptions.Compiled)]
    private static partial Regex WordBoundary { get; }

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is null)
            return string.Empty;

        var enumText = value.ToString() ?? string.Empty;
        if (enumText.Length == 0)
            return string.Empty;

        var withSpaces = WordBoundary.Replace(enumText, "$1 $2");

        return char.ToUpperInvariant(withSpaces[0])
            + withSpaces[1..].ToLowerInvariant();
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotImplementedException();    
}

public class EnumEqualsConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is null || parameter is null)
            return false;

        var enumType = value.GetType();
        if (!enumType.IsEnum)
            return false;

        var parsed = Enum.Parse(enumType, parameter.ToString()!, ignoreCase: true);
        return value.Equals(parsed);
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is not bool isChecked || !isChecked || parameter is null || !targetType.IsEnum)
            return Binding.DoNothing;

        return Enum.Parse(targetType, parameter.ToString()!, ignoreCase: true);
    }
}

public class DivideDoubleConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is not double number || number <= 0)
            return 0d;

        if (parameter is null || !double.TryParse(parameter.ToString(), out var divisor) || divisor == 0)
            return number;

        return number / divisor;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotImplementedException();
}

public class ActivityIntensityToBrushConverter : IValueConverter
{
    private static readonly Brush[] FallbackBluePalette =
    [
        CreateFrozenBrush("#FF17191D"),
        CreateFrozenBrush("#FF0A2A4A"),
        CreateFrozenBrush("#FF0F4C7D"),
        CreateFrozenBrush("#FF1772B8"),
        CreateFrozenBrush("#FF1E95EC"),
        CreateFrozenBrush("#FF4AB2FF"),
        CreateFrozenBrush("#FF86D0FF")
    ];

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is not int intensity)
            return FallbackBluePalette[0];

        intensity = Math.Clamp(intensity, 0, 6);

        var presetPrefix = parameter as string;
        if (string.IsNullOrWhiteSpace(presetPrefix))
            presetPrefix = "ActivityHeatBlue";

        string key = $"{presetPrefix}{intensity}Brush";

        if (App.Current.Resources[key] is Brush configuredBrush)
            return configuredBrush;

        return FallbackBluePalette[intensity];
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotImplementedException();

    private static Brush CreateFrozenBrush(string hex)
    {
        var brush = (SolidColorBrush)new BrushConverter().ConvertFromString(hex)!;
        brush.Freeze();
        return brush;
    }
}

public class ActivityWeekToMonthLabelConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is not IEnumerable<ActivityCellVM> days)
            return string.Empty;

        var monthStart = days.FirstOrDefault(d => d.Date.Day == 1);
        if (monthStart is null)
            return string.Empty;

        string format = parameter as string ?? "MMM";
        return monthStart.Date.ToDateTime(TimeOnly.MinValue)
            .ToString(format, culture);
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
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

public class ToLearningStagesDomain : IValueConverter<LearningStagesVM, LearningStages>
{
    public LearningStages Convert(LearningStagesVM sourceMember, ResolutionContext context)
        => new(sourceMember.I, 
               sourceMember.II, 
               sourceMember.III);
}

public class ToLearningStagesVM : IValueConverter<LearningStages, LearningStagesVM>
{
    public LearningStagesVM Convert(LearningStages sourceMember, ResolutionContext context)
        => new(sourceMember);
}

public class TimeOnlyToUint : IValueConverter<TimeOnly, uint>
{
    public uint Convert(TimeOnly sourceMember, ResolutionContext context)
        => (uint)sourceMember.Hour;
}

public class UintToTimeOnly : IValueConverter<uint, TimeOnly>
{
    public TimeOnly Convert(uint sourceMember, ResolutionContext context)
        => new((int)sourceMember, 0);
}

public static class EFConverters
{
    public static ValueConverter<ImmutableList<long>, string> ImmutableLongList =>
        new(
            v => JsonSerializer.Serialize(v),
            v => JsonSerializer.Deserialize<ImmutableList<long>>(v) 
                ?? ImmutableList<long>.Empty
        );

    public static ValueConverter<ImmutableList<CardState>, string> ImmutableCardStateList =>
        new(
            v => JsonSerializer.Serialize(v),
            v => JsonSerializer.Deserialize<ImmutableList<CardState>>(v) 
                ?? ImmutableList<CardState>.Empty
        );
}

#endregion
