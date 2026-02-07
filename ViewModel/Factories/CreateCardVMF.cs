using FlashMemo.Repositories;
using FlashMemo.Services;
using FlashMemo.ViewModel.Windows;
using FlashMemo.ViewModel.Wrappers;

namespace FlashMemo.ViewModel.Factories;

public class CreateCardVMF(ICardService cs, ITagRepo tr, ICardRepo cr, DeckSelectVMF dsVMF)
{
    private readonly ICardService cardService = cs;
    private readonly ITagRepo tagRepo = tr;
    private readonly ICardRepo cardRepo = cr;
    private readonly DeckSelectVMF deckSelectVMF = dsVMF;

    public CreateCardVM Create(DeckNode targetDeck)
        => new(cardService, tagRepo, cardRepo, targetDeck, deckSelectVMF);
    
    // TODO: merge this with EditCardVMF if they still have same dependencies in the end.
}