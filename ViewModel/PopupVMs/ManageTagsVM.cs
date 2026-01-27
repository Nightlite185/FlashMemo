using System.Collections.ObjectModel;
using FlashMemo.Model.Persistence;
using FlashMemo.Repositories;
using FlashMemo.Helpers;
using FlashMemo.ViewModel.BaseVMs;

namespace FlashMemo.ViewModel.PopupVMs;

public partial class ManageTagsVM: PopupVMBase
{
    private ManageTagsVM(Func<IEnumerable<Tag>, bool, Task> confirm, 
        Action cancel, ITagRepo tr, long cardId, long userId): base(cancel)
    {
        this.confirm = confirm;
        this.tagRepo = tr;
        this.cardId = cardId;
        this.userId = userId;
    }

    /* Calling confirm should save card's tags, but editing global tags 
    gets saved immediately in the db using tagRepo. Also, every time 
    when editing global tags, you MUST flip the globalTagsEdited bool to true
    so the BrowseVM reloads everything that used tags.
    
    TODO: CardTags and AllTags should use a wrapper vm for notifying the UI
    bc the observable collection does not know about tag properties being changed,
    unless the elements implement INPC, which Im gonna do here with ObservableProperty attr*/

    private async Task InitializeAsync(long userId, long cardId)
    {
        CardTags.AddRange(
            await tagRepo.GetFromCardAsync(cardId));

        AllTags.AddRange(
            await tagRepo.GetFromUserAsync(userId));
    } 
    private readonly long cardId;
    private readonly long userId;
    private readonly ITagRepo tagRepo;
    private readonly Func<IEnumerable<Tag>, bool, Task> confirm;
    public readonly ObservableCollection<Tag> CardTags = [];
    public readonly ObservableCollection<Tag> AllTags = [];
    private bool globalTagsEdited = false;

    public override async Task Confirm() => await confirm(CardTags, globalTagsEdited);
    public async static Task<ManageTagsVM> CreateAsync(Func<IEnumerable<Tag>, bool, Task> confirm, 
        Action cancel, ITagRepo tr, long cardId, long userId)
    {
        ManageTagsVM vm = new(
            confirm, cancel, tr,
            cardId, userId
        );
        
        await vm.InitializeAsync(userId, cardId);
        return vm;
    }
}