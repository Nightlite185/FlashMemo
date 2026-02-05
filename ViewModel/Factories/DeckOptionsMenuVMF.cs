using AutoMapper;
using FlashMemo.Model.Persistence;
using FlashMemo.Repositories;
using FlashMemo.Services;
using FlashMemo.ViewModel.Windows;

namespace FlashMemo.ViewModel.Factories;

public class DeckOptionsMenuVMF(IMapper m, IDeckOptVMBuilder doVMB, IDeckOptionsRepo dor)
{
    private readonly IMapper mapper = m;
    private readonly IDeckOptionsRepo deckOptRepo = dor;
    private readonly IDeckOptVMBuilder deckOptVMBuilder = doVMB;

    public async Task<DeckOptionsMenuVM> CreateAsync(Deck deck)
    {
        DeckOptionsMenuVM vm = new(mapper, deckOptVMBuilder, deckOptRepo, deck);

        await vm.InitializeAsync();
        return vm;
    }
}