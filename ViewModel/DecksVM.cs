using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using FlashMemo.Helpers;
using FlashMemo.Model.Persistence;
using FlashMemo.Repositories;
using FlashMemo.Services;

namespace FlashMemo.ViewModel
{
    public interface IViewModel;
    public partial class DecksVM: ObservableObject, IViewModel
    {
        public DecksVM(WindowService ws, CardQueryService cqs, DeckRepo dr, DeckTreeBuilder dtr)
        {
            deckTreeBuilder = dtr;
            windowService = ws;
            cardQueryS = cqs;
            deckRepo = dr;

            Decks = [];
        }

        #region methods
        public async Task LoadUser(long userId)
        {
            this.userId = userId;
            await SyncDeckTree();
        }
        private async Task SyncDeckTree()
        {
            if (!UserLoaded) throw new InvalidOperationException(
                "Cannot sync deck tree without loading the user first.");

            Decks.Clear();

            // build root-level decks without parents (ParentId == null)
            var deckTree = await deckTreeBuilder.BuildCountedAsync((long)userId!);

            Decks.AddRange(deckTree);
        }
        #endregion

        #region public properties
        public ObservableCollection<DeckNode> Decks { get; init; }
        
        [ObservableProperty]
        public partial DeckNode? SelectedDeck { get; private set; }
        #endregion

        #region private things
        private readonly WindowService windowService;
        private readonly CardQueryService cardQueryS;
        private readonly DeckRepo deckRepo;
        private readonly DeckTreeBuilder deckTreeBuilder;
        private long? userId;
        private bool UserLoaded => userId is not null;
        #endregion
    }
}