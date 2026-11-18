using CommunityToolkit.Mvvm.ComponentModel;
using FlashMemo.Model.Domain;
using FlashMemo.Services;
using Microsoft.Extensions.Logging;

namespace FlashMemo.ViewModel.Other;

public partial class AnswerRatioVM(long userId, IStatsQueryService statsService,
                                    ILogger<AnswerRatioVM> logger, IDialogService dialogs, SafetyNet net)
                                    : ObservableObject, IViewModel
{
    private bool revertingPeriod, revertingAnswer;

    internal async Task InitAsync()
    {
        // Initialization failures propagate to the factory.
        PercentageRatio = await LoadRatioAsync(Answer, LastPeriod);
    }

    private async Task<bool> TryUpdateRatio()
    {
        var answer = Answer;
        var period = LastPeriod;
        var newRatio = await net.TryUnsafe(
            () => LoadRatioAsync(answer, period),
            actionTried: "load the answer ratio for the selected answer type and time period",
            retry: true);

        if (newRatio.Success)
            PercentageRatio = newRatio.Value;

        return newRatio.Success;
    }

    private Task<int> LoadRatioAsync(Answers answer, TimePeriod period)
        => statsService.GetAnswerRatio(answer, period, userId);

    #region public properties
    [ObservableProperty]
    public partial Answers Answer { get; set; } = Answers.Good;

    [ObservableProperty]
    public partial TimePeriod LastPeriod { get; set; } = TimePeriod.Month;

    [ObservableProperty]
    public partial int PercentageRatio { get; private set; }
    #endregion

    async partial void OnLastPeriodChanged(TimePeriod oldValue, TimePeriod newValue)
    {
        if (!revertingPeriod && !await TryUpdateRatio())
        {
            revertingPeriod = true;
            LastPeriod = oldValue;
            revertingPeriod = false;
        }
    }

    async partial void OnAnswerChanged(Answers oldValue, Answers newValue)
    {
        if (!revertingAnswer && !await TryUpdateRatio())
        {
            revertingAnswer = true;
            Answer = oldValue;
            revertingAnswer = false;
        }
    }
}

public enum TimePeriod { Day, Week, Month, Year }