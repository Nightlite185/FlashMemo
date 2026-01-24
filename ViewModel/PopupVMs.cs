using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FlashMemo.Helpers;
using FlashMemo.Model.Domain;
using FlashMemo.Model.Persistence;
using FlashMemo.Repositories;

namespace FlashMemo.ViewModel
{
    public abstract partial class PopupVMBase
        (Action cancel): ObservableObject
    {
        private readonly Action cancel = cancel;

        [RelayCommand]
        public void Cancel() => cancel();

        [RelayCommand]
        public abstract Task Confirm();
    }
    public partial class PostponeVM 
        (Func<int, bool, Task> confirm, Action cancel)
        : PopupVMBase(cancel)
    {
        public override async Task Confirm() => await confirm(PostponeByDays, KeepInterval);
        private readonly Func<int, bool, Task> confirm = confirm;

        [ObservableProperty]
        public partial bool KeepInterval { get; set; }

        [ObservableProperty]
        public partial int PostponeByDays { get; set; }
    }

    public partial class RescheduleVM 
        (Func<DateTime, bool, Task> confirm, Action cancel)
        : PopupVMBase(cancel)
    {
        public override async Task Confirm() => await confirm(RescheduleToDate, KeepInterval);
        private readonly Func<DateTime, bool, Task> confirm = confirm;

        [ObservableProperty]
        public partial DateTime RescheduleToDate { get; set; }
        
        [ObservableProperty]
        public partial bool KeepInterval { get; set; }
    }

    public partial class ManageTagsVM: PopupVMBase
    {
        private ManageTagsVM(Func<IEnumerable<Tag>, bool, Task> confirm, 
            Action cancel, TagRepo tr, long cardId, long userId): base(cancel)
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
        unless the elements implement INPC, which Im gonna do here with ObservableObject attr*/

        private async Task InitializeAsync(long userId, long cardId)
        {
            CardTags.AddRange(
                await tagRepo.GetFromCardAsync(cardId));

            AllTags.AddRange(
                await tagRepo.GetFromUserAsync(userId));
        } 
        private readonly long cardId;
        private readonly long userId;
        private readonly TagRepo tagRepo;
        private readonly Func<IEnumerable<Tag>, bool, Task> confirm;
        public readonly ObservableCollection<Tag> CardTags = [];
        public readonly ObservableCollection<Tag> AllTags = [];
        private bool globalTagsEdited = false;

        public override async Task Confirm() => await confirm(CardTags, globalTagsEdited);
        public async static Task<ManageTagsVM> CreateAsync(Func<IEnumerable<Tag>, bool, Task> confirm, 
            Action cancel, TagRepo tr, long cardId, long userId)
        {
            ManageTagsVM vm = new(
                confirm, cancel, tr,
                cardId, userId
            );
            
            await vm.InitializeAsync(userId, cardId);
            return vm;
        }
    }

    public partial class MoveCardsVM
        (Func<Deck, Task> confirm, Action cancel)
        : PopupVMBase(cancel)
    {
        public override async Task Confirm() => await confirm(ChosenDeckNode.Deck);

        [ObservableProperty]
        public partial DeckNode ChosenDeckNode { get; set; }
    }
}