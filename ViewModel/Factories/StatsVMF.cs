using FlashMemo.Services;
using FlashMemo.ViewModel.Other;
using FlashMemo.ViewModel.Windows;

namespace FlashMemo.ViewModel.Factories;

public class StatsVMF(IStatsQueryService statsService, IVMEventBus bus)
{
    public async Task<StatsVM> CreateAsync(long userId)
    {
        AnswerRatioVM ratioVM = new(
            userId, statsService);

        await ratioVM.InitAsync();

        StatsVM sVM = new(
            bus, userId, ratioVM, 
            statsService);

        await sVM.InitAsync();
        return sVM;
    }
}