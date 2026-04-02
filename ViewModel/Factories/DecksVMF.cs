using FlashMemo.Repositories;
using FlashMemo.Services;
using FlashMemo.ViewModel.Windows;

namespace FlashMemo.ViewModel.Factories;

public class DecksVMF(IDeckRepo deckRepo, IDeckTreeBuilder deckBuilder, 
                      IVMEventBus bus, ActivityGridVMF activityFactory)
{
    public async Task<DecksVM> CreateAsync(long userId)
    {
        var activityGrid = await activityFactory
            .CreateAsync(userId);

        var vm = new DecksVM(
            deckRepo, deckBuilder,
            activityGrid, userId, bus
        );

        await vm.InitAsync();
        return vm;
    }
}