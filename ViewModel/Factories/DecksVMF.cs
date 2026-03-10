using FlashMemo.Repositories;
using FlashMemo.Services;
using FlashMemo.ViewModel.Windows;

namespace FlashMemo.ViewModel.Factories;

public class DecksVMF(IDeckRepo dr, IDeckTreeBuilder dtb, IVMEventBus bus)
{
    private readonly IDeckRepo deckRepo = dr;
    private readonly IVMEventBus eventBus = bus;
    private readonly IDeckTreeBuilder deckTreeBuilder = dtb;

    public async Task<DecksVM> CreateAsync(long userId)
    {
        var vm = new DecksVM(
            deckRepo, deckTreeBuilder,
            userId, eventBus
        );

        await vm.SyncDeckTree();
        return vm;
    }
}