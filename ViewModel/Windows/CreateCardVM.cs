using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FlashMemo.Helpers;
using FlashMemo.Model.Persistence;
using FlashMemo.Repositories;
using FlashMemo.Services;
using FlashMemo.ViewModel.Bases;
using FlashMemo.ViewModel.Factories;
using FlashMemo.ViewModel.Wrappers;

namespace FlashMemo.ViewModel.Windows;

public partial class CreateCardVM(ICardService cs, ITagRepo tr, ICardRepo cr, IDeckMeta targetDeck, DeckSelectVMF dsVMF)
: EditorVMBase(cs, tr, cr), ICloseRequest, IPopupHost
{
    #region public properties
    [ObservableProperty]
    public partial NewCardVM WipCard { get; private set; } = new();
    
    [ObservableProperty]
    public partial IDeckMeta CurrentDeck { get; set; } = targetDeck;

    [ObservableProperty]
    public partial PopupVMBase? CurrentPopup { get; set; }
    #endregion

    #region methods
    private async Task ChangeDeck(IDeckMeta newDeck)
    {
        if (CurrentDeck.Id == newDeck.Id)
            return;

        CurrentDeck = newDeck;
    }
    private void ClosePopup()
    {
        CurrentPopup = null;
    }
    #endregion

    #region ICommands
    [RelayCommand]
    protected async Task AddCard()
    {
        var card = CardEntity.CreateNew(
            WipCard.FrontContent,
            WipCard.BackContent,
            CurrentDeck.Id,
            WipCard.Tags.ToEntities());

        await cardRepo.AddCard(card);

        WipCard = new();
    }
    
    [RelayCommand]
    private async Task ShowDeckSelect()
    {
        CurrentPopup = await deckSelectVMF.CreateAsync(
            ChangeDeck, ClosePopup,
            CurrentDeck.UserId);
    }
    #endregion

    private readonly DeckSelectVMF deckSelectVMF = dsVMF;
}