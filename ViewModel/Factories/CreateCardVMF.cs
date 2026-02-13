using FlashMemo.Model.Persistence;
using FlashMemo.Repositories;
using FlashMemo.Services;
using FlashMemo.ViewModel.Windows;

namespace FlashMemo.ViewModel.Factories;

public class CreateCardVMF(ICardService cs, ITagRepo tr, ICardRepo cr, DeckSelectVMF dsVMF, ILastSessionService lss)
{
    private readonly ICardService cardService = cs;
    private readonly ITagRepo tagRepo = tr;
    private readonly ICardRepo cardRepo = cr;
    private readonly DeckSelectVMF deckSelectVMF = dsVMF;
    private readonly ILastSessionService lastSession = lss;

    public CreateCardVM Create(IDeckMeta targetDeck)
    {
        return new(cardService, tagRepo,
            cardRepo, targetDeck,
            deckSelectVMF, lastSession);
    }

    // TODO: merge this with EditCardVMF if they still have same dependencies in the end.
}