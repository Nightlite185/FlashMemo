using AutoMapper;
using FlashMemo.Repositories;
using FlashMemo.Services;
using FlashMemo.ViewModel.Windows;

namespace FlashMemo.ViewModel.Factories;

public class DeckOptionsMenuVMF(IMapper mapper, IDeckOptVMBuilder deckOptVMB, IDeckOptionsService deckOptService, IDeckRepo deckRepo, IVMEventBus bus)
{
    public async Task<DeckOptionsMenuVM> CreateAsync(long deckId)
    {
        var deck = await deckRepo.GetById(deckId);

        DeckOptionsMenuVM vm = new(
            mapper, deckOptVMB, 
            deckOptService, deck, bus);

        await vm.InitializeAsync();
        return vm;
    }
}