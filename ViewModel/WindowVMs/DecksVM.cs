using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using FlashMemo.Helpers;
using FlashMemo.Repositories;
using FlashMemo.Services;
using FlashMemo.ViewModel.WrapperVMs;

namespace FlashMemo.ViewModel.WindowVMs;

public partial class DecksVM: ObservableObject, IViewModel
{
    public DecksVM(IWindowService ws, ICardQueryService cqs, IDeckRepo dr, IDeckTreeBuilder dtr)
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
        IEnumerable<DeckNode> deckTree = await 
            deckTreeBuilder.BuildCountedAsync((long)userId!);

        Decks.AddRange(deckTree);
    }
    #endregion

    #region public properties
    public ObservableCollection<DeckNode> Decks { get; init; }
    
    [ObservableProperty]
    public partial DeckNode? SelectedDeck { get; private set; }
    #endregion

    #region private things
    private readonly IWindowService windowService;
    private readonly ICardQueryService cardQueryS;
    private readonly IDeckRepo deckRepo;
    private readonly IDeckTreeBuilder deckTreeBuilder;
    private long? userId;
    private bool UserLoaded => userId is not null;
    #endregion
}
