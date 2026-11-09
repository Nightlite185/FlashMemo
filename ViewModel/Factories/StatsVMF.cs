using FlashMemo.Services;
using FlashMemo.ViewModel.Other;
using FlashMemo.ViewModel.Windows;
using Microsoft.Extensions.Logging;

namespace FlashMemo.ViewModel.Factories;

public class StatsVMF(IStatsQueryService statsService, IVMEventBus bus, ILogger<StatsVM> logger, 
                    ILogger<AnswerRatioVM> answerRatioLogger, IDialogService dialogs, SafetyNet net)
{
    public async Task<StatsVM?> TryCreateAsync(long userId)
    {
        var res = await net.TryUnsafe(
            action: async () =>
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
            },
            actionTried: "open the statistics view",
            retry: true);

        return res.Success ? res.Value : null;
    }
}