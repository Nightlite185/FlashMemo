using System.Runtime.CompilerServices;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FlashMemo.Helpers;
using FlashMemo.Model.Persistence;
using FlashMemo.Repositories;
using FlashMemo.Services;
using FlashMemo.ViewModel.Popups;
using FlashMemo.ViewModel.Factories;
using FlashMemo.ViewModel.Wrappers;

namespace FlashMemo.ViewModel;

public partial class CardCtxMenuVM(ICardService cs, ICardRepo cr, ManageTagsVMF mtVMF, IPopupHost pph, 
                                DeckSelectVMF dsVMF, IDomainEventBus eventBus, long userId): ObservableObject
{
    #region ICommands
    
    [RelayCommand]
    private async Task MoveCardsCtx()
    {
        popupHost.CurrentPopup = await deckSelectVMF
            .CreateAsync(MoveCards, PopupCancel, userId);
    }

    [RelayCommand]
    private void ShowRescheduleCtx() // this lets u choose datetime
    {
        popupHost.CurrentPopup = new RescheduleVM(RescheduleCards, PopupCancel);
    }

    [RelayCommand]
    private void ShowPostponeCtx() // this moves due date by specified num of days. Choose if keep interval or change to num of days choosen.
    {                             // also you can choose if postpone by days SINCE today or SINCE card's DUE DATE
        popupHost.CurrentPopup = new PostponeVM(PostponeCards, PopupCancel);
    }

    [RelayCommand]
    private async Task ForgetCardsCtx()
    {
        await ModifyCardsHelper(
            c => c.Forget(),
            CardAction.Forget
        );
    }

    [RelayCommand]
    private async Task ToggleBuryCtx()
    {
        await ModifyCardsHelper(
            c => c.FlipBuried(),
            CardAction.Bury
        );
    }

    [RelayCommand]
    private async Task ToggleSuspendedCtx()
    {
        await ModifyCardsHelper(
            c => c.FlipSuspended(),
            CardAction.Suspend
        );
    }

    [RelayCommand]
    private async Task DeleteCardsCtx()
    {
        ThrowIfNoCardsCaptured(nameof(DeleteCardsCtxCommand));

        if (capturedCards!.First() is CardVM)
        {
            foreach (var vm in capturedCards!.Cast<CardVM>())
                vm.IsDeleted = true;
        }
        
        var ids = capturedCards!.Select(c => c.Id);

        await cardRepo.DeleteCards(ids);
        await eventBus.Notify(new CardsDeletedArgs(ids));
    }

    [RelayCommand(CanExecute = nameof(CanExecuteIfOneCard))]
    private async Task ManageTags() // ONLY VISIBLE IF ONE CARD IS SELECTED
    {
        ThrowIfNotOneCaptured();

        long cardId = capturedCards!
            .Single().Id;

        popupHost.CurrentPopup = await manageTagsVMF.CreateAsync(
            confirm: ChangeTags,
            cancel: PopupCancel,
            cardId, userId
        );
    }
    [RelayCommand(CanExecute = nameof(CanExecuteIfOneCard))]
    private async Task ShowCardDetails()
    {
        ThrowIfNotOneCaptured();

        // show it here
        throw new NotImplementedException();
    }
    
    #endregion
    
    #region methods
    public void OpenMenu(IReadOnlyCollection<ICardVM> selected)
    {
        if (capturedCards is not null)
            throw new InvalidOperationException(
            "Cannot capture cards if there already are some captured. Clean those up first.");

        capturedCards = [..selected];
    }

    ///<summary> Called when user closed ctx menu without clicking on any option </summary>
    internal void CloseMenu() => capturedCards = null;
    private void ThrowIfNoCardsCaptured(string? calledMember)
    {
        if (capturedCards is null || capturedCards.Count == 0)
            throw new InvalidOperationException(
            $"Called {calledMember}, but there are no captured cards yet.");
    }
    private void ThrowIfNotOneCaptured([CallerMemberName] string? caller = null)
    {
        if (capturedCards?.Count != 1)
            throw new InvalidOperationException(
            $"To execute this there needs to be only one card captured, but there was {(capturedCards is null ? "null" : capturedCards.Count)}");
    }
    private void PopupCancel()
    {
        CloseMenu();
        popupHost.CurrentPopup = null;
    }
    private async Task ModifyCardsHelper(Action<CardEntity> cardModifier, CardAction cardAction, [CallerMemberName] string? caller = null)
    {
        ThrowIfNoCardsCaptured(caller);

        var entities = capturedCards!.ToEntities();

        foreach (var card in entities)
            cardModifier(card);

        await cardService.SaveEditedCards(
            entities, cardAction);

        await RaiseBasedOnAction(entities, cardAction);
    }
    private async Task RescheduleCards(DateTime dt, bool keepInterval)
    {
        await ModifyCardsHelper(
            c => c.Reschedule(dt, keepInterval),
            CardAction.Reschedule);
    }
    private async Task PostponeCards(int PostponeBy, bool keepInterval)
    {
        await ModifyCardsHelper(
            c => c.Postpone(PostponeBy, keepInterval),
            CardAction.Reschedule
        );
    }
    private async Task MoveCards(IDeckMeta newDeck)
    {
        await ModifyCardsHelper(
            c => c.MoveToDeck(newDeck.Id),
            CardAction.Relocate
        );
    }
    private async Task ChangeTags(IEnumerable<Tag> newTags, bool globalTagsEdited)
    {
        await ModifyCardsHelper(
            c => c.ReplaceTagsWith(newTags),
            CardAction.Modify
        );

        //if (globalTagsEdited)
            // raise event about sth modified.

        PopupCancel();
    }

    private async Task RaiseBasedOnAction(IEnumerable<CardEntity> cards, CardAction action)
    {
        switch (action)
        {
            case CardAction.Reschedule or CardAction.Forget:
                await eventBus.Notify(new CardsRescheduledArgs(cards));
                break;

            case CardAction.Bury or CardAction.Suspend:
                await eventBus.Notify(new CardsSuspendBuryArgs(cards));
                break;

            case CardAction.Relocate:
                await eventBus.Notify(new CardsRelocatedArgs(
                    cards.Select(c => c.Id),
                    cards.First().DeckId));
                break;
        }
    }
    #endregion
    
    #region private things
    private bool CanExecuteIfOneCard => capturedCards?.Count == 1;
    private readonly ICardService cardService = cs;
    private readonly DeckSelectVMF deckSelectVMF = dsVMF;
    private readonly IDomainEventBus eventBus = eventBus;
    private readonly ICardRepo cardRepo = cr;
    private readonly ManageTagsVMF manageTagsVMF = mtVMF;
    private readonly IPopupHost popupHost = pph;
    private IReadOnlyCollection<ICardVM>? capturedCards;
    private long userId = userId;
    #endregion
}