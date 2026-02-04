using System.Formats.Asn1;
using FlashMemo.Model.Persistence;
using FlashMemo.Repositories;
using FlashMemo.Services;
using FlashMemo.ViewModel.Windows;

namespace FlashMemo.ViewModel.Factories;

public class CreateCardVMF(ICardService cs, ITagRepo tr, ICardRepo cr)
{
    private readonly ICardService cardService = cs;
    private readonly ITagRepo tagRepo = tr;
    private readonly ICardRepo cardRepo = cr;

    public CreateCardVM Create(Deck targetDeck) 
        => new(cardService, tagRepo, cardRepo, targetDeck);
    
    // TODO: merge this with EditCardVMF if they still have same dependencies in the end.
}