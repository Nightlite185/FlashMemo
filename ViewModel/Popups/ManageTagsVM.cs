using System.Collections.ObjectModel;
using FlashMemo.Model.Persistence;
using FlashMemo.Repositories;
using FlashMemo.Helpers;
using FlashMemo.ViewModel.Bases;
using FlashMemo.ViewModel.Wrappers;

namespace FlashMemo.ViewModel.Popups;

public sealed partial class ManageTagsVM: PopupVMBase
{
    internal ManageTagsVM(Func<IEnumerable<Tag>, bool, Task> confirm,
        Action cancel, ITagRepo tr, long cardId, long userId): base(cancel)
    {
        this.confirm = confirm;
        this.tagRepo = tr;
        this.cardId = cardId;
        this.userId = userId;
    }

    //* Calling confirm should save card's tags, but editing global tags 
    //* gets saved immediately in the db using tagRepo. Also, every time
    //! when editing global tags, you MUST flip the globalTagsEdited bool to true
    //* so the BrowseVM reloads everything that used tags.

    internal async Task InitializeAsync(long userId, long cardId)
    {
        var cardTags = await tagRepo.GetFromCard(cardId);
        var allTags = await tagRepo.GetFromUser(userId);

        CardTags.AddRange(cardTags.ToVMs());
        AllTags.AddRange(allTags.ToVMs());
    }
    private readonly long cardId;
    private readonly long userId;
    private readonly ITagRepo tagRepo;
    private readonly Func<IEnumerable<Tag>, bool, Task> confirm;
    public readonly ObservableCollection<TagVM> CardTags = [];
    public readonly ObservableCollection<TagVM> AllTags = [];
    private bool globalTagsEdited;

    protected override async Task Confirm()
    {
        await confirm(
            CardTags.Select(vm => vm.ToEntity()), 
            globalTagsEdited);

        Close();
    }
}