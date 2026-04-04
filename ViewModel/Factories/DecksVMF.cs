using FlashMemo.Repositories;
using FlashMemo.Services;
using FlashMemo.ViewModel.Windows;

namespace FlashMemo.ViewModel.Factories;

public class DecksVMF(IDeckRepo deckRepo, IDeckTreeBuilder deckBuilder, 
                      IVMEventBus bus, HeatmapVMF heatVMF)
{
    public async Task<DecksVM> CreateAsync(long userId)
    {
        var heatVM = await heatVMF
            .CreateAsync(userId);

        var vm = new DecksVM(
            deckRepo, deckBuilder,
            heatVM, userId, bus
        );

        await vm.InitAsync();
        return vm;
    }
}