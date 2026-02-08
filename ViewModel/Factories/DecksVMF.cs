using FlashMemo.Repositories;
using FlashMemo.Services;
using FlashMemo.ViewModel.Windows;

namespace FlashMemo.ViewModel.Factories;

public class DecksVMF(IDeckRepo dr, IDeckTreeBuilder dtb)
{
    private readonly IDeckRepo deckRepo = dr;
    private readonly IDeckTreeBuilder deckTreeBuilder = dtb;

    public async Task<DecksVM> CreateAsync(long userId)
    {
        var vm = new DecksVM(
            deckRepo, deckTreeBuilder,
            userId
        );

        await vm.SyncDeckTree();
        return vm;
    }
}