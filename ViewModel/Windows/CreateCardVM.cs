using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FlashMemo.Helpers;
using FlashMemo.Model.Persistence;
using FlashMemo.Repositories;
using FlashMemo.Services;
using FlashMemo.ViewModel.Bases;
using FlashMemo.ViewModel.Wrappers;

namespace FlashMemo.ViewModel.Windows;

public partial class CreateCardVM(ICardService cs, ITagRepo tr, ICardRepo cr, Deck targetDeck)
: EditorVMBase(cs, tr, cr), ICloseRequest
{
    [ObservableProperty]
    public partial NewCardVM WipCard { get; private set; } = new(targetDeck);

    [RelayCommand]
    protected async Task AddCard()
    {
        var card = CardEntity.CreateNew(
            WipCard.FrontContent,
            WipCard.BackContent,
            WipCard.Deck.Id,
            WipCard.Tags.ToEntities());

        await cardRepo.AddCard(card);

        WipCard = new(card.Deck);
    }
}