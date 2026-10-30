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
                                IDeckRepo deckRepo, DeckSelectVMF deckSelectVMF, EditableCardVM cardVM,
                                ILogger<CardEditorVM> log, IDialogService dialogService)
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
    
    public async Task OnActionExecuted(CtxMenuAction action)
    {
        switch (action)
        {
            case CtxMenuAction.Delete:
                OnCloseRequest?.Invoke();
                break;

            case CtxMenuAction.Relocate:
                var result = await TryUnsafe(
                    () => deckRepo.GetFromCard(Card.Id),
                    actionTried: "load the card's current deck",
                    retry: true);

                if (!result.Success)
                    return;

                Card.ChangeDeck(result.Value);
                break;
        }
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
        await cardService.SaveEditedCard(
            Card.ToEntity(),
            CardAction.Modify
        );

        eventBus.NotifyDomain();
        OnCloseRequest?.Invoke();
    }

    [RelayCommand] private async Task RevertChanges()
        => Card.RevertChanges();

    [RelayCommand] private void CancelChanges()
        => OnCloseRequest?.Invoke();

    [RelayCommand] private void ShowCtxMenu()
        => CtxMenuVM.OpenMenu([Card]);

    [RelayCommand] private async Task ShowDeckSelect()
    {
        var result = await TryUnsafe(
            () => deckSelectVMF.CreateAsync(
                ChangeDeck, CancelPopup, Card.Deck.UserId),
            actionTried: "load the available decks",
            retry: true);

        if (!result.Success)
            return;

        CurrentPopup = result.Value;
    }
    #endregion

    public event Action? OnCloseRequest;
}