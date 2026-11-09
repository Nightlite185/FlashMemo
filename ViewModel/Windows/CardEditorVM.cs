using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FlashMemo.Model.Persistence;
using FlashMemo.Repositories;
using FlashMemo.Services;
using FlashMemo.ViewModel.Bases;
using FlashMemo.ViewModel.Factories;
using FlashMemo.ViewModel.Other;
using FlashMemo.ViewModel.Wrappers;
using Microsoft.Extensions.Logging;

namespace FlashMemo.ViewModel.Windows;

public partial class CardEditorVM(ICardService cardService, ICardRepo cardRepo, IVMEventBus eventBus, 
                                DeckSelectVMF deckSelectVMF, EditableCardVM cardVM,
                                ILogger<CardEditorVM> log, IDialogService dialogService, SafetyNet net)
                                : BaseVM(eventBus, log, dialogService), ICloseRequest, IPopupHost, ICtxMenuHost, ICardTagsVMHost
{
    public List<TagVM> Tags => Card.Tags;
    public EditableCardVM Card { get; private set; } = cardVM;
    public CardCtxMenuVM CtxMenuVM { get; private set; } = null!;
    public ICardTagsVM CardTagsVM { get; private set; } = null!;

    [ObservableProperty]
    public partial PopupVMBase? CurrentPopup { get; set; }

    #region methods
    internal void Initialize(CardCtxMenuVM ccmVM, ICardTagsVM ctVM) //* Factory calls this
    {
        CardTagsVM = ctVM;
        CtxMenuVM = ccmVM;
        
        eventBus.DomainChanged += OnDomainChanged;
    }
    
    public Task OnActionExecuted(CtxMenuAction action, IReadOnlyCollection<CardEntity> affectedCards)
    {
        var saved = affectedCards
            .Single(c => c.Id == Card.Id);
            
        if (action != CtxMenuAction.Delete)
            Card.UpdateLastSaved(saved);

        switch (action)
        {
            case CtxMenuAction.Delete:
                OnCloseRequest?.Invoke();
                break;

            case CtxMenuAction.Relocate:
                Card.ChangeDeck(saved.Deck);
                break;
        }

        return Task.CompletedTask;
    }

    protected override async Task ReloadDomainAsync()
    {
        var card = await cardRepo
            .GetById(Card.Id);

        if (card is null)
        {
            OnCloseRequest?.Invoke();
            return;
        }
    } 
    
    // Popup confirmation callback: let failures propagate to the popup's SafetyNet.
    // Do not handle failures here; the popup closes only when this callback succeeds.
    private async Task ChangeDeck(IDeckMeta deck)
    {
        Card.ChangeDeck(deck);

        await Task.CompletedTask;
    }
    private void CancelPopup() => CurrentPopup = null;
    #endregion

    #region ICommands
    [RelayCommand] private async Task SaveChanges()
    {
        var edited = Card.ToEntity();

        Task action() => cardService.SaveEditedCard(
            edited, CardAction.Modify);

        if (!await net.TryUnsafe(action,
            actionTried: "save the modified card.",
            retry: false))
        {
            return;
        }

        Card.UpdateLastSaved(edited);
        eventBus.NotifyDomain();
        OnCloseRequest?.Invoke();
    }

    [RelayCommand] private async Task RevertChanges()
        => Card.RevertChanges();

    [RelayCommand] private void CancelChanges()
        => OnCloseRequest?.Invoke();

    [RelayCommand] private void ShowCtxMenu()
        => CtxMenuVM.OpenMenu([Card.SavedEntity]);

    [RelayCommand] private async Task ShowDeckSelect()
    {
        var vm = await deckSelectVMF.TryCreateAsync(
            ChangeDeck, CancelPopup, Card.Deck.UserId);
        if (vm is null) return;

        CurrentPopup = vm;
    }
    #endregion

    public event Action? OnCloseRequest;
}