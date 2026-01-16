using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using FlashMemo.Model.Persistence;
using FlashMemo.Repositories;
using FlashMemo.Services;

namespace FlashMemo.ViewModel
{
    public interface IViewModel;
    public partial class MainVM: ObservableObject, IViewModel
    {
        public MainVM(WindowService ws, CardQueryService cqs, DeckRepo dr)
        {
            windowService = ws;
            cardQueryS = cqs;
            deckRepo = dr;

            Decks = [];
        }

        #region methods
        public async Task LoadUser(long userId) 
        {
            this.userId = userId;
            
            cardsCount = await 
                cardQueryS.CountCardsAsync(
                    userId, 
                    countOnlyStudyable: true
                );
            
            decksLookup = await deckRepo
                .BuildDeckLookupAsync(userId);

            DeckLookupToTree();
        }
        private void DeckLookupToTree()
        {
            Decks.Clear();

            // build root-level decks without parents (parent id == null)
            var rootVMs = BuildDeckLevel(parentId: null);

            foreach (var VM in rootVMs)
                Decks.Add(VM);
        }
        
        private IEnumerable<DeckItemVM> BuildDeckLevel(long? parentId)
        {
            foreach (var deck in decksLookup[parentId])
            {
                // build children recursively
                var children = BuildDeckLevel(deck.Id);

                // get card counts for this deck
                if (!cardsCount.TryGetValue(deck.Id, out var cc))
                {
                    throw new InvalidOperationException(
                        $"{nameof(cardsCount)} deck ids don't match the ones in {nameof(decksLookup)}");
                }

                // create the VM
                yield return new DeckItemVM(deck, cc, children);
            }
        }

        #endregion

        #region public properties
        public ObservableCollection<DeckItemVM> Decks { get; init; }
        
        [ObservableProperty]
        public partial DeckItemVM? SelectedDeck { get; private set; }
        
        [ObservableProperty]
        public partial object? CurrentView { get; private set; }
        #endregion

        #region private things
        private readonly WindowService windowService;
        private readonly CardQueryService cardQueryS;
        private readonly DeckRepo deckRepo;
        private long? userId;
        private bool UserLoaded => userId is not null;
        private ILookup<long?, Deck> decksLookup = null!; // remember to invalidate this and replace when hierarchy changes, or when user clicks refresh button
        private IDictionary<long, CardsCount> cardsCount = null!; // this as well
        #endregion
    }
}