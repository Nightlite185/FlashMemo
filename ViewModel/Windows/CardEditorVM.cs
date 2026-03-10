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

public partial class CardEditorVM(ICardService cs, ITagRepo tr, ICardRepo cr, IVMEventBus bus, IDeckRepo dr, DeckSelectVMF dsVMF)
                                : BaseVM(bus), ICloseRequest, IPopupHost, ICtxMenuHost
{
    public EditableCardVM Card { get; private set; } = null!;
    public CardCtxMenuVM CtxMenuVM { get; private set; } = null!;

    [ObservableProperty]
    public partial PopupVMBase? CurrentPopup { get; set; }

    #region methods
    internal async Task Initialize(long cardId, CardCtxMenuVM ccmVM) //* Factory calls this
    {
        CtxMenuVM = ccmVM;
        eventBus.DomainChanged += OnDomainChanged;

        var card = await cardRepo.GetById(cardId);
        var tags = await tagRepo.GetFromCard(cardId);

        card.Tags.AddRange(tags); // snapshotting old tags
        Card = new(card);
    }
    
    public async Task OnActionExecuted(CtxMenuAction action)
    {
        switch (action)
        {
            case CtxMenuAction.Delete:
                OnCloseRequest?.Invoke();
                break;

            case CtxMenuAction.Relocate:
                Card.ChangeDeck(await deckRepo
                    .GetFromCard(Card.Id));
                break;
        }
    }

    protected override async Task ReloadDomainAsync()
        => Card = new( await cardRepo.GetById(Card.Id));

    private async Task ChangeDeck(IDeckMeta deck)
    {
        Card.ChangeDeck(deck);

        await Task.CompletedTask;
    }
    private void CancelPopup() => CurrentPopup = null;
    #endregion

    #region private things
    private readonly ITagRepo tagRepo = tr;
    private readonly ICardService cardService = cs;
    private readonly ICardRepo cardRepo = cr;
    private readonly IDeckRepo deckRepo = dr;
    private readonly DeckSelectVMF deckSelectVMF = dsVMF;
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
        => CurrentPopup = await deckSelectVMF.CreateAsync(
            ChangeDeck, CancelPopup, Card.Deck.UserId);
    #endregion

    public event Action? OnCloseRequest;
}