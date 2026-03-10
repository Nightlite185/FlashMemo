using FlashMemo.Model.Persistence;
using FlashMemo.Repositories;
using FlashMemo.Services;
using FlashMemo.ViewModel.Windows;

namespace FlashMemo.ViewModel.Factories;

public class CreateCardVMF(ICardService cs, ITagRepo tr, ICardRepo cr, DeckSelectVMF dsVMF, 
                            ILastSessionService lss, IVMEventBus bus, IDeckRepo repo)
{
    private readonly ICardService cardService = cs;
    private readonly IVMEventBus eventBus = bus;
    private readonly ITagRepo tagRepo = tr;
    private readonly ICardRepo cardRepo = cr;
    private readonly DeckSelectVMF deckSelectVMF = dsVMF;
    private readonly ILastSessionService lastSession = lss;
    private readonly IDeckRepo deckRepo = repo;

    public CreateCardVM Create(IDeckMeta targetDeck)
    {
        var vm = new CreateCardVM(
            cardService, tagRepo,
            cardRepo, targetDeck,
            deckSelectVMF, lastSession,
            eventBus, deckRepo
        );

        vm.Initialize();
        return vm;
    }
}