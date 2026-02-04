using FlashMemo.Helpers;
using FlashMemo.Repositories;
using FlashMemo.Services;
using FlashMemo.ViewModel.Windows;

namespace FlashMemo.ViewModel.Factories;

public class DecksVMF(IWindowService ws, ICardQueryService cqs, IDeckRepo dr, IDeckTreeBuilder dtb)
{
    private readonly IWindowService windowService = ws;
    private readonly ICardQueryService cardQueryService = cqs;
    private readonly IDeckRepo deckRepo = dr;
    private readonly IDeckTreeBuilder deckTreeBuilder = dtb;

    public async Task<DecksVM> CreateAsync(long userId)
    {
        var vm = new DecksVM(
            windowService, cardQueryService,
            deckRepo, deckTreeBuilder, userId
        );

        await vm.SyncDeckTree();
        return vm;
    }
}