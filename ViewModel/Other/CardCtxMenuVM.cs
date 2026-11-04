using AutoMapper;
using System.Runtime.CompilerServices;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FlashMemo.Model.Persistence;
using FlashMemo.Repositories;
using FlashMemo.Services;
using FlashMemo.ViewModel.Popups;
using FlashMemo.ViewModel.Factories;
using Microsoft.Extensions.Logging;

namespace FlashMemo.ViewModel.Other;

public enum CtxMenuAction { Relocate, Reschedule, Forget, Bury, Suspend, Delete }

public partial class CardCtxMenuVM(ICardService cs, ICardRepo cr, IPopupHost pph, DeckSelectVMF dsVMF,
                                IVMEventBus eventBus, IUserOptionsService userOptionsService,
                                long userId, ICtxMenuHost host, ILogger<CardCtxMenuVM> logger,
                                IDialogService dialogs, SafetyNet net, IMapper mapper, 
                                RescheduleVMF rescheduleVMF): ObservableObject
{
    #region ICommands
    
    [RelayCommand]
    private async Task MoveCardsCtx()
    {
        var result = await net.TryUnsafe(
            () => deckSelectVMF.CreateAsync(MoveCards, PopupCancel, userId),
            actionTried: "load the available decks",
            retry: true);

        if (result.Success)
            popupHost.CurrentPopup = result.Value;
    }

    [RelayCommand]
    private void ShowRescheduleCtx() // this lets u choose datetime OR postpone by days. Choose whether keep interval. 
    {
        popupHost.CurrentPopup = rescheduleVMF.Create(
            confirm: RescheduleCards, cancel: PopupCancel);
    }

    [RelayCommand]
    private async Task ForgetCardsCtx()
    {
        await net.TryUnsafe(
            () => ModifyCardsHelper(c => c.Forget(), CardAction.Forget, CtxMenuAction.Forget),
            actionTried: "forget the selected cards",
            retry: false);
    }

    [RelayCommand]
    private async Task ToggleBuryCtx()
    {
        await net.TryUnsafe(
            () => ModifyCardsHelper(c => c.FlipBuried(), CardAction.Bury, CtxMenuAction.Bury),
            actionTried: "change whether the selected cards are buried",
            retry: false);
    }

    [RelayCommand]
    private async Task ToggleSuspendedCtx()
    {
        await net.TryUnsafe(
            () => ModifyCardsHelper(c => c.FlipSuspended(), CardAction.Suspend, CtxMenuAction.Suspend),
            actionTried: "change whether the selected cards are suspended",
            retry: false);
    }

    [RelayCommand]
    private async Task DeleteCardsCtx()
    {
        ThrowIfNoCardsCaptured(nameof(DeleteCardsCtxCommand));

        var selected = capturedCards!;
        var cardCount = selected.Count;
        bool shouldConfirm = true;

        try
        {
            shouldConfirm = (await userOptionsService
                .GetFromUser(userId)).ConfirmCardDeletion;
        }
        
        catch (Exception e)
        {
            logger.LogError(e,
                "Failed to load card deletion preferences for user {UserId}; asking for confirmation anyway",
                userId);
        }

        if (shouldConfirm)
        {
            var subject = cardCount == 1
                ? "this card"
                : $"these {cardCount} cards";

            var answer = dialogs.Show(
                title: cardCount == 1 ? "Delete card?" : "Delete cards?",
                message: $"Are you sure you want to permanently delete {subject}? "
                    + "This action can't be undone."
                    + DialogService.DeleteConfirmationSettingsHint,
                buttons: DialogButtons.YesNo,
                icon: DialogIcons.Warning);

            if (answer is DialogResult.No)
                return;
        }

        var ids = selected.Select(card => card.Id).ToHashSet();
        if (!await net.TryUnsafe(
            () => cardRepo.DeleteCards(ids),
            actionTried: "delete the selected cards",
            retry: false))
            return;

        capturedCards = [..capturedCards!.Where(card => !ids.Contains(card.Id))];
        await ctxHost.OnActionExecuted(CtxMenuAction.Delete, selected);
        eventBus.NotifyDomain();
    }
    #endregion
    
    #region methods
    // Capture owned baselines immediately. No caller-owned entity is modified by this menu.
    public void OpenMenu(IReadOnlyCollection<CardEntity> selected)
        => capturedCards = mapper.Map<List<CardEntity>>(selected);

    ///<summary> Called when user closed ctx menu without clicking on any option </summary>
    private void ThrowIfNoCardsCaptured(string? calledMember)
    {
        if (capturedCards is null || capturedCards.Count == 0)
            throw new InvalidOperationException(
            $"Called {calledMember}, but there are no captured cards yet.");
    }
    private void PopupCancel()
        => popupHost.CurrentPopup = null;

    private async Task ModifyCardsHelper(Action<CardEntity> cardModifier, CardAction cardAction,
        CtxMenuAction ctxAction, Func<CardEntity, bool>? filter = null, [CallerMemberName] string? caller = null)
    {
        ThrowIfNoCardsCaptured(caller);

        // Every attempt starts from unchanged baselines, so a failed move/postpone can be confirmed again.
        // Keep failures propagating to the command/popup boundary; success callbacks belong after the save.
        var entities = mapper.Map<List<CardEntity>>(
            capturedCards!.Where(card => filter?.Invoke(card) ?? true));

        if (entities.Count == 0)
            return;

        foreach (var card in entities)
            cardModifier(card);

        await cardService.SaveEditedCards(
            entities, cardAction);

        var savedById = entities.ToDictionary(card => card.Id);
        capturedCards = [..capturedCards!.Select(card => savedById.GetValueOrDefault(card.Id, card))];
        await ctxHost.OnActionExecuted(ctxAction, entities);
        eventBus.NotifyDomain();
    }
    
    // Popup confirmation callback: let failures propagate to the popup's SafetyNet.
    // Do not handle failures here; the popup closes only when this callback succeeds.
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
            CardAction.Reschedule, CtxMenuAction.Reschedule);
    }

    // Popup confirmation callback: let failures propagate to the popup's SafetyNet.
    // Do not handle failures here; the popup closes only when this callback succeeds.
    private async Task MoveCards(IDeckMeta newDeck)
    {
        var target = newDeck.ToEntity();
        await ModifyCardsHelper(
            c => c.MoveToDeck(target),
            CardAction.Relocate, CtxMenuAction.Relocate,
            filter: card => card.DeckId != target.Id);
    }
    #endregion
    
    #region private things
    private readonly ICardService cardService = cs;
    private readonly DeckSelectVMF deckSelectVMF = dsVMF;
    private readonly IVMEventBus eventBus = eventBus;
    private readonly IUserOptionsService userOptionsService = userOptionsService;
    private readonly ICardRepo cardRepo = cr;
    private readonly IPopupHost popupHost = pph;
    private readonly ICtxMenuHost ctxHost = host;
    private IReadOnlyCollection<CardEntity>? capturedCards;
    private long userId = userId;
    #endregion
}