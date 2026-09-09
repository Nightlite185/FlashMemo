using CommunityToolkit.Mvvm.ComponentModel;
using FlashMemo.Services;
using FlashMemo.ViewModel.Bases;
using FlashMemo.ViewModel.Other;

namespace FlashMemo.ViewModel.Windows;

public partial class StatsVM (IVMEventBus bus, long userId, AnswerRatioVM ansRatioVM, 
                                IStatsQueryService statsService) : BaseVM(bus)
{
    private const string NotAvailable = "N/A";

    internal async Task InitAsync()
    {
        WeekDayWithMostReviewsInLastMonth = (await statsService
            .DayWithMostReviewsInLastMonth(userId)) is DayOfWeek d
                ? d.ToString()
                : NotAvailable;

        var ts = await statsService.AvgAnswerTimeInLastMonth(userId);

        AverageAnswerTimeInLastMonth = (ts == TimeSpan.MinValue)
            ? NotAvailable
            : FormatDuration(ts);

        int hr = await statsService.MostReviewedHourOfDayInLastMonth(userId);

        MostReviewedHourOfDayInLastMonth = hr == int.MinValue
            ? NotAvailable
            : $"{hr:00}:00";

        TotalReviewsEver = await statsService
            .TotalReviewsEver(userId);

        CurrentStreak = await statsService
            .CurrentReviewStreak(userId);

        LongestStreak = await statsService
            .LongestReviewStreak(userId);
    }

    private static string FormatDuration(TimeSpan duration)
    {
        return duration.TotalHours >= 1
            ? $"{(int)duration.TotalHours:00}:{duration.Minutes:00}:{duration.Seconds:00}"
            : $"{(int)duration.TotalMinutes:00}:{duration.Seconds:00}";
    }

    [ObservableProperty]
    public partial string AverageAnswerTimeInLastMonth { get; private set; } = NotAvailable;

    [ObservableProperty]
    public partial string WeekDayWithMostReviewsInLastMonth { get; private set; } = NotAvailable;

    [ObservableProperty]
    public partial string MostReviewedHourOfDayInLastMonth { get; private set; } = NotAvailable;

    [ObservableProperty]
    public partial int TotalReviewsEver { get; private set; }
    
    [ObservableProperty]
    public partial int CurrentStreak { get; private set; }
    
    [ObservableProperty]
    public partial int LongestStreak { get; private set; }

    public AnswerRatioVM AnswerRatioVM { get; } = ansRatioVM;
}
