using System.Runtime.CompilerServices;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FlashMemo.Helpers;
using FlashMemo.Model.Persistence;
using FlashMemo.Repositories;
using FlashMemo.Services;
using FlashMemo.ViewModel.PopupVMs;
using FlashMemo.ViewModel.VMFactories;
using FlashMemo.ViewModel.WrapperVMs;

namespace FlashMemo.ViewModel;

[Flags] public enum ReloadTypes
{
    Tags, Cards, DeckTree
}

public partial class CardCtxMenuVM(ICardService cs, ICardRepo cr, ManageTagsVMF mtVMF, IPopupHost pph, IReloadHandler rh, long userId): ObservableObject
{
    #region ICommands
    //* IMPORTANT: commands that open a window -> dont get async,
    //* but those that directly call async internal methods and use services -> should

    [RelayCommand]
    public void MoveCardsCtx()
    {
        popupHost.CurrentPopup = new MoveCardsVM(MoveCards, PopupCancel);
    }

    [RelayCommand]
    public void ShowRescheduleCtx() // this lets u choose datetime
    {
        popupHost.CurrentPopup = new RescheduleVM(RescheduleCards, PopupCancel);
    }

    [RelayCommand]
    public void ShowPostponeCtx() // this moves due date by specified num of days. Choose if keep interval or change to num of days choosen.
    {                                    // also you can choose if postpone by days SINCE today or SINCE card's DUE DATE 
        popupHost.CurrentPopup = new PostponeVM(PostponeCards, PopupCancel);
    }

    [RelayCommand]
    public async Task ForgetCardsCtx()
    {
        await ModifyCardsHelper(
            c => c.Forget(),
            CardAction.Forget
        );
    }

    [RelayCommand]
    public async Task ToggleBuryCtx()
    {
        await ModifyCardsHelper(
            c => c.FlipBuried(),
            CardAction.Bury
        );
    }

    [RelayCommand]
    public async Task ToggleSuspendedCtx()
    {
        await ModifyCardsHelper(
            c => c.FlipSuspended(),
            CardAction.Suspend
        );
    }

    [RelayCommand]
    public async Task DeleteCardsCtx()
    {
        foreach (var vm in capturedCards!)
        {
            vm.IsDeleted = true;
            vm.NotifyUI();
        }
        
        await cardRepo.DeleteCards(
            capturedCards.Select(vm => vm.ToEntity()));
    }

    [RelayCommand]
    public async Task ManageTags() // ONLY VISIBLE IF ONE CARD IS SELECTED
    {
        
        long cardId = capturedCards!
            .Single().Id;

        popupHost.CurrentPopup = await manageTagsVMF.CreateAsync(
            confirm: ChangeTags,
            cancel: PopupCancel,
            cardId, loadedUserId
        );
    }
    
    #endregion
    
    #region methods
    public void OpenMenu(IReadOnlyCollection<CardItemVM> selected)
    {
        if (capturedCards is not null)
            throw new InvalidOperationException(
            "Cannot capture cards if there already are some captured. Clean those up first.");

        capturedCards = [..selected];
    }

    ///<summary> Called this when user closed ctx menu without clicking on any option </summary>
    public void CloseMenu() => capturedCards = null;
    private void ThrowIfNoCardsCaptured(string? calledMember)
    {
        if (capturedCards is null || capturedCards.Count == 0)
            throw new InvalidOperationException(
            $"Called {calledMember}, but there are no captured cards yet.");
    }
    private void PopupCancel()
    {
        CloseMenu();
        popupHost.CurrentPopup = null;
    }
    private async Task ModifyCardsHelper(Action<CardEntity> cardModifier, CardAction cardAction, [CallerMemberName] string? caller = null)
    {
        ThrowIfNoCardsCaptured(caller);

        foreach (var vm in capturedCards!)
        {
            cardModifier(vm.ToEntity());
            vm.NotifyUI();
        }

        await cardService.SaveEditedCards(
            capturedCards.ToEntities(),
            cardAction
        );
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
    private async Task MoveCards(Deck newDeck)
    {
        await ModifyCardsHelper(
            c => c.MoveToDeck(newDeck),
            CardAction.Relocate
        );
    }
    private async Task ChangeTags(IEnumerable<Tag> newTags, bool globalTagsEdited)
    {
        await ModifyCardsHelper(
            c => c.ReplaceTagsWith(newTags),
            CardAction.Modify
        );

        if (globalTagsEdited)
            await reloadHandler.ReloadAsync(ReloadTypes.Tags | ReloadTypes.Cards);

        PopupCancel();
    }
    #endregion
    
    #region private things
    private readonly ICardService cardService = cs;
    private readonly ICardRepo cardRepo = cr;
    private readonly ManageTagsVMF manageTagsVMF = mtVMF;
    private readonly IPopupHost popupHost = pph;
    private readonly IReloadHandler reloadHandler = rh;
    private IReadOnlyCollection<CardItemVM>? capturedCards;
    private long loadedUserId = userId;
    #endregion
}