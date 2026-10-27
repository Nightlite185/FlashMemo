using CommunityToolkit.Mvvm.ComponentModel;
using FlashMemo.Model.Domain;
using FlashMemo.Services;
using Microsoft.Extensions.Logging;

namespace FlashMemo.ViewModel.Other;

public partial class AnswerRatioVM(long userId, IStatsQueryService statsService,
                                    ILogger<AnswerRatioVM> logger, IDialogService dialogs)
                                    : ObservableObject, IViewModel
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
    public partial Answers Answer { get; set; } = Answers.Good;

    [ObservableProperty]
    public partial TimePeriod LastPeriod { get; set; } = TimePeriod.Month;

    [ObservableProperty]
    public partial int PercentageRatio { get; private set; }
    #endregion

    async partial void OnLastPeriodChanged(TimePeriod value)
        => await UpdateRatio();

    async partial void OnAnswerChanged(Answers value)
        => await UpdateRatio();
}

public enum TimePeriod { Day, Week, Month, Year }