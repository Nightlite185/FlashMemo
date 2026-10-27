using FlashMemo.Services;
using FlashMemo.ViewModel.Other;
using Microsoft.Extensions.Logging;

namespace FlashMemo.ViewModel.Factories;

public class HeatmapVMF(IActivityVMBuilder builder, IUserOptionsService userOptService,
                        IVMEventBus bus, ILogger<HeatmapVM> logger, IDialogService dialogs)
{
    public async Task<HeatmapVM> CreateAsync(long userId)
    {
        var vm = new HeatmapVM(
            builder, userOptService, 
            bus, userId,
            logger, dialogs);

        await vm.InitAsync();
        return vm;
    }
}