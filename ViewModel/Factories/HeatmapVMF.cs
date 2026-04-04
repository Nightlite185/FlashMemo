using FlashMemo.Services;
using FlashMemo.ViewModel.Other;

namespace FlashMemo.ViewModel.Factories;

public class HeatmapVMF(IActivityVMBuilder builder, IUserOptionsService userOptService,
                            IVMEventBus bus)
{
    public async Task<HeatmapVM> CreateAsync(long userId)
    {
        var vm = new HeatmapVM(
            builder, userOptService, 
            bus, userId);

        await vm.InitAsync();
        return vm;
    }
}