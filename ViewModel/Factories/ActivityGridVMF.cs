using FlashMemo.Services;
using FlashMemo.ViewModel.Other;

namespace FlashMemo.ViewModel.Factories;

public class ActivityGridVMF(IActivityVMBuilder builder)
{
    public async Task<ActivityGridVM> CreateAsync(long userId)
    {
        var vm = new ActivityGridVM(
            builder, userId);

        await vm.ReloadAsync();
        return vm;
    }
}