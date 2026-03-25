using System.Runtime.CompilerServices;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FlashMemo.Helpers;
using FlashMemo.Model.Persistence;
using FlashMemo.Repositories;
using FlashMemo.Services;
using FlashMemo.ViewModel.Popups;
using FlashMemo.ViewModel.Factories;

namespace FlashMemo.ViewModel.Other;

public enum CtxMenuAction { Relocate, Reschedule, Forget, Bury, Suspend, Delete, ShowDetails }

public partial class CardCtxMenuVM(ICardService cs, ICardRepo cr, IPopupHost pph, DeckSelectVMF dsVMF, 
                                IVMEventBus eventBus, long userId, ICtxMenuHost host): ObservableObject
{
    #region ICommands
    
    [RelayCommand]
    private async Task MoveCardsCtx()
    {
        popupHost.CurrentPopup = await deckSelectVMF
            .CreateAsync(MoveCards, PopupCancel, userId);
    }

    [RelayCommand]
    private void ShowRescheduleCtx() // this lets u choose datetime OR postpone by days. Choose whether keep interval. 
    {
        popupHost.CurrentPopup = new RescheduleVM(RescheduleCards, PopupCancel);
    }

    [RelayCommand]
    private async Task ForgetCardsCtx()
    {
        await ModifyCardsHelper(
            c => c.Forget(),
            CardAction.Forget
        );

        await ctxHost.OnActionExecuted(CtxMenuAction.Forget);
    }

    [RelayCommand]
    private async Task ToggleBuryCtx()
    {
        await ModifyCardsHelper(
            c => c.FlipBuried(),
            CardAction.Bury
        );

        await ctxHost.OnActionExecuted(CtxMenuAction.Bury);
    }

    [RelayCommand]
    private async Task ToggleSuspendedCtx()
    {
        await ModifyCardsHelper(
            c => c.FlipSuspended(),
            CardAction.Suspend
        );

        await ctxHost.OnActionExecuted(CtxMenuAction.Suspend);
    }

    [RelayCommand]
    private async Task DeleteCardsCtx()
    {
        ThrowIfNoCardsCaptured(nameof(DeleteCardsCtxCommand));

        var ids = capturedCards!.Select(c => c.Id);

        await cardRepo.DeleteCards(ids);
        eventBus.NotifyDomain();

        await ctxHost.OnActionExecuted(CtxMenuAction.Delete);
    }
    #endregion
    
    #region methods
    public void OpenMenu(IReadOnlyCollection<ICardVM> selected)
        => capturedCards = [..selected];

    ///<summary> Called when user closed ctx menu without clicking on any option </summary>
    private void ThrowIfNoCardsCaptured(string? calledMember)
    {
        if (capturedCards is null || capturedCards.Count == 0)
            throw new InvalidOperationException(
            $"Called {calledMember}, but there are no captured cards yet.");
    }
    private void PopupCancel()
        => popupHost.CurrentPopup = null;

    private async Task ModifyCardsHelper(Action<CardEntity> cardModifier, CardAction cardAction, [CallerMemberName] string? caller = null)
    {
        ThrowIfNoCardsCaptured(caller);

        var entities = capturedCards!.ToEntities();

        foreach (var card in entities)
            cardModifier(card);

        await cardService.SaveEditedCards(
            entities, cardAction);

        eventBus.NotifyDomain();
    }
    private async Task RescheduleCards(IRescheduleData data)
    {
        Action<CardEntity> action = data switch
        {
            RescheduleData rd => c => c.Reschedule(rd),
            PostponeData pd => c => c.Postpone(pd),

            _ => throw new ArgumentException(
                "provided data is neither RescheduleData nor PostponeData",
                nameof(data))
        };
        
        await ModifyCardsHelper(
            action,
            CardAction.Reschedule);

        await ctxHost.OnActionExecuted(CtxMenuAction.Reschedule);
    }
    private async Task MoveCards(IDeckMeta newDeck)
    {
        ThrowIfNoCardsCaptured(nameof(MoveCards));

        //* only take cards that arent already in that deck.
        capturedCards = [..capturedCards!
            .Where(c => c.DeckId != newDeck.Id)];

        //* if all of them are in it -> return quickly.
        if (capturedCards.Count == 0) 
            return;

        await ModifyCardsHelper(
            c => c.MoveToDeck(newDeck.ToEntity()),
            CardAction.Relocate
        );

        await ctxHost.OnActionExecuted(CtxMenuAction.Relocate);
    }
    private async Task ChangeTags(IEnumerable<Tag> newTags)
    {
        await ModifyCardsHelper(
            c => c.ReplaceTagsWith(newTags),
            CardAction.Modify
        );

        PopupCancel();
    }
    #endregion
    
    #region private things
    private readonly ICardService cardService = cs;
    private readonly DeckSelectVMF deckSelectVMF = dsVMF;
    private readonly IVMEventBus eventBus = eventBus;
    private readonly ICardRepo cardRepo = cr;
    private readonly IPopupHost popupHost = pph;
    private readonly ICtxMenuHost ctxHost = host;
    private IReadOnlyCollection<ICardVM>? capturedCards;
    private long userId = userId;
    #endregion
}