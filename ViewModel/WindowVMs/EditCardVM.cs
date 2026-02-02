using CommunityToolkit.Mvvm.Input;
using FlashMemo.Helpers;
using FlashMemo.Model.Persistence;
using FlashMemo.Repositories;
using FlashMemo.Services;
using FlashMemo.ViewModel.BaseVMs;

namespace FlashMemo.ViewModel.WindowVMs;

public partial class EditCardVM(ICardService cs, ITagRepo tr, ICardRepo cr)
: EditorVMBase(cs, tr, cr), ICloseRequest
{
    #region methods
    internal async Task Initialize(long cardId) // Factory must call this
    {
        lastSavedCard = await cardRepo.GetCard(cardId);

        var tags = await tagRepo.GetFromCard(cardId);
        lastSavedCard.Tags.AddRange(tags); // snapshotting old tags
        
        CardVM = new (lastSavedCard);
        Tags.AddRange(tags.ToVMs());
    }
    protected override async Task SaveChanges()
    {
        await base.SaveChanges();
        Close();
    }
    #endregion

    #region private things
    private CardEntity lastSavedCard = null!;
    #endregion
    
    #region ICommands
    [RelayCommand]
    private async Task RevertChanges() // only reverts the vm-made changes to what the card was.
    {
        CardVM.FrontContent = lastSavedCard.FrontContent;
        CardVM.BackContent = lastSavedCard.BackContent ?? "";

        Tags.Clear();
        var oldTagIds = lastSavedCard.Tags;

        //* refreshing the tags cuz user might have edited them globally
        //* after loading this VM, but before this command was called.
        var oldTags = await tagRepo.GetByIds(
            oldTagIds.Select(t => t.Id));
            
        Tags.AddRange(oldTags.ToVMs());
    }
    #endregion
}