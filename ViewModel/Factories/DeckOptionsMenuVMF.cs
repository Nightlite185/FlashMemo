using AutoMapper;
using FlashMemo.Repositories;
using FlashMemo.Services;
using FlashMemo.ViewModel.Windows;
using Microsoft.Extensions.Logging;

namespace FlashMemo.ViewModel.Factories;

public class DeckOptionsMenuVMF(IMapper mapper, IDeckOptVMBuilder deckOptVMB,
                                IDeckOptionsService deckOptService, IDeckRepo deckRepo,
                                IVMEventBus bus, IUserOptionsService userOptionsService,
                                ILogger<DeckOptionsMenuVM> logger, IDialogService dialogs, SafetyNet net)
{
    public async Task<DeckOptionsMenuVM> CreateAsync(long deckId)
    {
        var deck = await deckRepo.GetById(deckId)
            ?? throw new ArgumentException(
            "Deck with provided id does not exist in the db.");

        DeckOptionsMenuVM vm = new(
            mapper, deckOptVMB, 
            deckOptService, userOptionsService,
            deck, bus,
            logger, dialogs, net);

        await vm.InitializeAsync();
        return vm;
    }
}
