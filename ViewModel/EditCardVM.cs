using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FlashMemo.Helpers;
using FlashMemo.Model.Persistence;
using FlashMemo.Repositories;
using FlashMemo.Services;

namespace FlashMemo.ViewModel
{
    public partial class EditCardVM: ObservableObject, IViewModel
    {
        public EditCardVM(CardService cs, TagRepo tr)
        {
            cardService = cs;
            tagRepo = tr;

            Tags = [];
            AllTags = [];
        }

        #region public properties
        [ObservableProperty]
        public partial string FrontContent { get; set; } = "";

        [ObservableProperty]
        public partial string BackContent { get; set; } = "";

        public ObservableCollection<Tag> Tags { get; init; }
        public ObservableCollection<Tag> AllTags { get; init; }
        #endregion

        #region methods
        public async Task Initialize(CardEntity card, long currentUserId)
        {
            this.card = card;
            this.userId = currentUserId;
            
            Tags.Clear(); // clearing for safety, just in case.
            Tags.AddRange(card.Tags);

            AllTags.Clear(); // here too

            var all = await tagRepo
                .GetAllFromUserAsync(currentUserId);

            AllTags.AddRange(all);
        }
        private void ThrowIfNotInitialized(string calledMember)
        {
            if (!Initialized) throw new InvalidOperationException(
                $"Tried to call {nameof(calledMember)} without calling {nameof(Initialize)}() first"
            );
        }
        #endregion

        #region private things
        private readonly CardService cardService;
        
        [ObservableProperty]
        private partial CardEntity? card { get; set; }

        private bool Initialized 
            => card is not null && userId is not null;
        private readonly TagRepo tagRepo;
        private long? userId;
        #endregion
        
        #region ICommands
        [RelayCommand]
        private async Task SaveChanges()
        {
            ThrowIfNotInitialized(nameof(SaveChanges));

            card!.Tags = [..Tags];
            card!.FrontContent = FrontContent;
            card!.BackContent = BackContent;

            await cardService.SaveEditedCard(card, CardAction.Modify);

            OnCloseRequested?.Invoke();
        }

        [RelayCommand]
        private void RevertChanges() // only reverts the vm-made changes to what the card was.
        {
            ThrowIfNotInitialized(nameof(RevertChanges));

            FrontContent = card!.FrontContent;
            BackContent = card!.BackContent ?? "";
            
            Tags.Clear();
            Tags.AddRange(card!.Tags);
        }

        [RelayCommand]
        private void CancelChanges() // cancels changes and closes the editor grid/window
        {
            ThrowIfNotInitialized(nameof(CancelChanges));
            OnCloseRequested?.Invoke();
        }
        #endregion

        public event Action? OnCloseRequested; // TO DO: find a clean way to wire this with window's handler
    }
}