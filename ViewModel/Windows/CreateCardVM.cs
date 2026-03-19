using System.Collections.ObjectModel;
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

public partial class CreateCardVM(ICardRepo cr, IDeckMeta targetDeck, 
DeckSelectVMF dsVMF, ILastSessionService lss, IVMEventBus bus, IDeckRepo repo)
: BaseVM(bus), ICloseRequest, IPopupHost, ICardTagsVMHost
{
    #region private things
    private const int HistoryCap = 10;
    private readonly DeckSelectVMF deckSelectVMF = dsVMF;
    private readonly ILastSessionService lastSession = lss;
    private readonly IDeckRepo deckRepo = repo;
    private readonly ICardRepo cardRepo = cr;
    #endregion
    
    #region public properties
    [ObservableProperty]
    public partial NewCardVM WipCard { get; private set; } = new();

    public List<TagVM> Tags => WipCard.Tags;
    
    [ObservableProperty]
    public partial IDeckMeta CurrentDeck { get; set; } = targetDeck;

    [ObservableProperty]
    public partial PopupVMBase? CurrentPopup { get; set; }

    public ObservableCollection<CardEntity> History { get; init; } = [];
    public ICardTagsVM CardTagsVM { get; private set; } = null!;
    public event Action? OnCloseRequest;
    #endregion

    #region methods
    internal void Initialize(ICardTagsVM ctVM)
    {
        CardTagsVM = ctVM;
        eventBus.DomainChanged += OnDomainChanged;
    }

    private async Task ChangeDeck(IDeckMeta newDeck)
    {
        if (CurrentDeck.Id == newDeck.Id)
            return;

        CurrentDeck = newDeck;
        lastSession.LastDeckId = newDeck.Id;
    }
    private void ClosePopup() => CurrentPopup = null;
    
    // TODO: test this, cant yet since all windows are dialogs for now.
    protected override async Task ReloadDomainAsync()
    {
        if (await deckRepo.Exists(CurrentDeck.Id))
            return;
        
        Deck? deck;

        if (lastSession.LastDeckId is long valid)
            deck = await deckRepo.GetById(valid);

        else deck = await deckRepo
            .GetFirst(CurrentDeck.UserId);

        // if its still null at this point, just close the window
        // since there are no decks on this user.
        if (deck is null) OnCloseRequest?.Invoke();
    }
    #endregion

    #region ICommands
    [RelayCommand]
    private void Close() => OnCloseRequest?.Invoke();

    [RelayCommand]
    private async Task AddCard()
    {
        var card = CardEntity.CreateNew(
            WipCard.Note.ToEntity(),
            CurrentDeck.Id,
            WipCard.Tags.ToEntities());

        await cardRepo.AddCard(card);

        History.Add(card);

        if (History.Count > HistoryCap)
            History.RemoveAt(0);

        WipCard = new();
        eventBus.NotifyDomain();
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
    #endregion
}