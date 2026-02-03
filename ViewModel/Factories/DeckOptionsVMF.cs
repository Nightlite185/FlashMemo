using AutoMapper;
using FlashMemo.Model.Persistence;
using FlashMemo.Repositories;
using FlashMemo.ViewModel.Windows;

namespace FlashMemo.ViewModel.Factories;

public class DeckOptionsVMF(IMapper m, IDeckOptionsRepo dor)
{
    private readonly IMapper mapper = m;
    private readonly IDeckOptionsRepo deckOptRepo = dor;

    public async Task<DeckOptionsMenuVM> CreateAsync(Deck deck)
    {
        DeckOptionsMenuVM vm = new(mapper, deckOptRepo, deck);

        await vm.InitializeAsync();
        return vm;
    }
}