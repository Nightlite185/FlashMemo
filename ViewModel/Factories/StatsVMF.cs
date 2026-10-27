using FlashMemo.Services;
using FlashMemo.ViewModel.Other;
using FlashMemo.ViewModel.Windows;
using Microsoft.Extensions.Logging;

namespace FlashMemo.ViewModel.Factories;

public class StatsVMF(IStatsQueryService statsService, IVMEventBus bus, ILogger<StatsVM> logger, 
                    ILogger<AnswerRatioVM> answerRatioLogger, IDialogService dialogs)
{
    public async Task<StatsVM> CreateAsync(long userId)
    {
        AnswerRatioVM ratioVM = new(
            userId, statsService,
            answerRatioLogger, dialogs);

        await ratioVM.InitAsync();

        StatsVM sVM = new(
            bus, userId, ratioVM, 
            statsService,
            logger, dialogs);

        await sVM.InitAsync();
        return sVM;
    }
}