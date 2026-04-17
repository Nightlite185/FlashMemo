using CommunityToolkit.Mvvm.ComponentModel;
using FlashMemo.Model.Domain;
using FlashMemo.Services;

namespace FlashMemo.ViewModel.Other;

public partial class AnswerRatioVM(long userId, IStatsQueryService statsService): ObservableObject, IViewModel
{
    internal async Task InitAsync()
    {
        await UpdateRatio();
    }

    private async Task UpdateRatio()
    {
        PercentageRatio = await statsService
            .GetAnswerRatio(Answer, LastPeriod, userId);
    }

    #region public propeties
    [ObservableProperty]
    public partial Answers Answer { get; private set; } = Answers.Good;

    [ObservableProperty]
    public partial TimePeriod LastPeriod { get; private set; } = TimePeriod.Month;

    [ObservableProperty]
    public partial int PercentageRatio { get; private set; }
    #endregion

    async partial void OnLastPeriodChanged(TimePeriod value)
        => await UpdateRatio();

    async partial void OnAnswerChanged(Answers value)
        => await UpdateRatio();
}

public enum TimePeriod { Day, Week, Month, Year }