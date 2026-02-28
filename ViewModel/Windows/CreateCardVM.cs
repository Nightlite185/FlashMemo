using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FlashMemo.Helpers;
using FlashMemo.Model;
using FlashMemo.Model.Persistence;
using FlashMemo.Repositories;
using FlashMemo.Services;
using FlashMemo.ViewModel.Bases;
using FlashMemo.ViewModel.Factories;
using FlashMemo.ViewModel.Wrappers;

namespace FlashMemo.ViewModel.Windows;

public partial class CreateCardVM(ICardService cs, ITagRepo tr, ICardRepo cr, 
IDeckMeta targetDeck, DeckSelectVMF dsVMF, ILastSessionService lss)
: NavBaseVM, ICloseRequest, IPopupHost
{
    #region private things
    private const int HistoryCap = 10;
    private readonly DeckSelectVMF deckSelectVMF = dsVMF;
    private readonly ILastSessionService lastSession = lss;
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

    public Queue<CardEntity> History { get; init; } = [];
    #endregion

    #region methods
    private async Task ChangeDeck(IDeckMeta newDeck)
    {
        if (CurrentDeck.Id == newDeck.Id)
            return;

        CurrentDeck = newDeck;
        lastSession.UpdateDeck(newDeck.Id);
    }
    private void ClosePopup() => CurrentPopup = null;
    #endregion

    #region ICommands
    [RelayCommand]
    private void Close() => OnCloseRequest?.Invoke();

    [RelayCommand]
    protected async Task AddCard()
    {
        var card = CardEntity.CreateNew(
            WipCard.Note.ToDomain(),
            CurrentDeck.Id,
            WipCard.Tags.ToEntities());

        await cardRepo.AddCard(card);

        History.Enqueue(card);

        if (History.Count > HistoryCap)
            History.Dequeue();

        WipCard = new();
    }
    
    [RelayCommand]
    private async Task ShowDeckSelect()
    {
        CurrentPopup = await deckSelectVMF.CreateAsync(
            ChangeDeck, ClosePopup,
            CurrentDeck.UserId);
    }

    [RelayCommand]
    private async Task ShowEditOnHistory(ICard card)
    {
        await NavigateTo(new EditCardNavRequest(
            card.Id, CurrentDeck.UserId));
    }

    public event Action? OnCloseRequest;
    #endregion
}