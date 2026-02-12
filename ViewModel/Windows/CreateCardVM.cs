using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FlashMemo.Helpers;
using FlashMemo.Model.Persistence;
using FlashMemo.Repositories;
using FlashMemo.Services;
using FlashMemo.ViewModel.Bases;
using FlashMemo.ViewModel.Factories;
using FlashMemo.ViewModel.Popups;
using FlashMemo.ViewModel.Wrappers;

namespace FlashMemo.ViewModel.Windows;

public partial class CreateCardVM(ICardService cs, ITagRepo tr, ICardRepo cr, IDeckMeta targetDeck, DeckSelectVMF dsVMF)
: ObservableObject, ICloseRequest, IPopupHost, IViewModel
{
    #region private things
    private readonly DeckSelectVMF deckSelectVMF = dsVMF;
    private readonly ICardService cardService = cs;
    private readonly ITagRepo tagRepo = tr;
    private readonly ICardRepo cardRepo = cr;
    #endregion
    
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
    private void Close() => OnCloseRequest?.Invoke();

    [RelayCommand]
    protected async Task AddCard()
    {
        var card = CardEntity.CreateNew(
            WipCard.FrontContentXAML,
            WipCard.BackContentXAML,
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

    public event Action? OnCloseRequest;
    #endregion
}