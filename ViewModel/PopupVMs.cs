using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FlashMemo.Helpers;
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
        (Func<int, Task> confirm, Action cancel)
        : PopupVMBase(cancel)
    {
        public override async Task Confirm() => await confirm(PostponeByDays);
        private readonly Func<int, Task> confirm = confirm;

        [ObservableProperty]
        public partial bool KeepInterval { get; set; }

        [ObservableProperty]
        public partial int PostponeByDays { get; set; }
    }

    public partial class RescheduleVM 
        (Func<DateTime, Task> confirm, Action cancel)
        : PopupVMBase(cancel)
    {
        public override async Task Confirm() => await confirm(RescheduleToDate);
        private readonly Func<DateTime, Task> confirm = confirm;

        [ObservableProperty]
        public partial DateTime RescheduleToDate { get; set; }
    }

    public partial class ManageTagsVM
        (Func<IEnumerable<Tag>, bool, Task> confirm, Action cancel, TagRepo tr, long cardId, long userId)
        : PopupVMBase(cancel)
    {
        /* Calling confirm should save card's tags, but editing global tags 
        gets saved immediately in the db using tagRepo. Also, every time 
        when editing global tags, you MUST flip the globalTagsEdited bool to true
        so the BrowseVM reloads everything that used tags. 
        
        TO DO: CardTags and AllTags should use a wrapper vm for notifying the UI
        bc the observable collection does not know about tag properties being changed,
        unless the elements implement INPC, which Im gonna do here with ObservableObject attr*/

        //TO DO: This MUST BE called in this class's ctor
        private async Task Initialize(long userId, long cardId)
        {
            CardTags.AddRange(
                await tagRepo.GetFromCardAsync(cardId));

            AllTags.AddRange(
                await tagRepo.GetFromUserAsync(userId));
        } 

        private readonly long card = cardId;
        private readonly long userId = userId;
        private readonly TagRepo tagRepo = tr;
        private readonly Func<IEnumerable<Tag>, bool, Task> confirm = confirm;
        public readonly ObservableCollection<Tag> CardTags = [];
        public readonly ObservableCollection<Tag> AllTags = [];
        private bool globalTagsEdited = false;

        public override async Task Confirm() => await confirm(CardTags, globalTagsEdited);
    }

    public partial class MoveCardsVM
        (Func<DeckNode, Task> confirm, Action cancel)
        : PopupVMBase(cancel)
    {
        public override async Task Confirm() => await confirm(ChosenDeck);

        [ObservableProperty]
        public partial DeckNode ChosenDeck { get; set; }
    }
}