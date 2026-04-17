using CommunityToolkit.Mvvm.ComponentModel;
using FlashMemo.Services;
using FlashMemo.ViewModel.Bases;
using FlashMemo.ViewModel.Other;

namespace FlashMemo.ViewModel.Windows;

public partial class StatsVM (IVMEventBus bus, long userId, AnswerRatioVM ansRatioVM, 
                                IStatsQueryService statsService) : BaseVM(bus)
{
    internal async Task InitAsync()
    {
        WeekDayWithMostReviewsInLastMonth = await statsService
            .DayWithMostReviewsInLastMonth(userId);

        AverageAnswerTimeInLastMonth = await statsService
            .AvgAnswerTimeInLastMonth(userId);

        MostReviewedHourOfDayInLastMonth = await statsService
            .MostReviewedHourOfDayInLastMonth(userId);

        TotalReviewsEver = await statsService
            .TotalReviewsEver(userId);
    }

    [ObservableProperty]
    public partial TimeSpan AverageAnswerTimeInLastMonth { get; private set; }

    [ObservableProperty]
    public partial DayOfWeek WeekDayWithMostReviewsInLastMonth { get; private set; }

    [ObservableProperty]
    public partial int MostReviewedHourOfDayInLastMonth { get; private set; }

    [ObservableProperty]
    public partial int TotalReviewsEver { get; private set; }

    public AnswerRatioVM AnswerRatioVM { get; } = ansRatioVM;
}