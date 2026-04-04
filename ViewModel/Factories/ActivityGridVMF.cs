using FlashMemo.Services;
using FlashMemo.ViewModel.Other;

namespace FlashMemo.ViewModel.Factories;

public class ActivityGridVMF(IActivityVMBuilder builder, IUserOptionsService userOptService,
                            IVMEventBus bus)
{
    public async Task<ActivityGridVM> CreateAsync(long userId)
    {
        var vm = new ActivityGridVM(
            builder, userOptService, 
            bus, userId);

        await vm.InitAsync();
        return vm;
    }
}