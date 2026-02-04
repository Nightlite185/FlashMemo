using CommunityToolkit.Mvvm.Input;
using FlashMemo.Helpers;
using FlashMemo.Model.Persistence;
using FlashMemo.Repositories;
using FlashMemo.Services;
using FlashMemo.ViewModel.Bases;
using FlashMemo.ViewModel.Wrappers;

namespace FlashMemo.ViewModel.Windows;

public partial class EditCardVM(ICardService cs, ITagRepo tr, ICardRepo cr)
: EditorVMBase(cs, tr, cr), ICloseRequest, IPopupHost, IReloadHandler
{
    public CardItemVM CardVM { get; protected set; } = null!; // factory sets this by calling initialize()
    public PopupVMBase? CurrentPopup { get; set; }

    #region methods
    internal async Task Initialize(long cardId, CardCtxMenuVM ccmVM) // Factory must call this
    {
        cardCtxMenuVM = ccmVM;
        lastSavedCard = await cardRepo.GetCard(cardId);

        var tags = await tagRepo.GetFromCard(cardId);
        lastSavedCard.Tags.AddRange(tags); // snapshotting old tags
        
        CardVM = new (lastSavedCard);
    }
    protected async Task SaveChanges()
    {
        var card = CardVM.ToEntity();
        card.ReplaceTagsWith(CardVM.Tags.ToEntities());

        await cardService.SaveEditedCard(card, CardAction.Modify);
        Close();
    }
    #endregion

    #region private things
    private CardEntity lastSavedCard = null!;
    private CardCtxMenuVM cardCtxMenuVM = null!;
    #endregion
    
    #region ICommands
    [RelayCommand]
    private async Task RevertChanges() // only reverts the vm-made changes to what the card was.
    {
        CardVM.FrontContent = lastSavedCard.FrontContent;
        CardVM.BackContent = lastSavedCard.BackContent ?? "";

        CardVM.Tags.Clear();
        var oldTagIds = lastSavedCard.Tags;

        //* refreshing the tags cuz user might have edited them globally
        //* after loading this VM, but before this command was called.
        var oldTags = await tagRepo.GetByIds(
            oldTagIds.Select(t => t.Id));
            
        CardVM.Tags.AddRange(oldTags.ToVMs());
    }

    public Task ReloadAsync(ReloadTargets rt)
    {
        throw new NotImplementedException();
    }

    
    #endregion
}