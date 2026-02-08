using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using FlashMemo.Helpers;
using FlashMemo.Repositories;
using FlashMemo.Services;
using FlashMemo.ViewModel.Wrappers;

namespace FlashMemo.ViewModel.Windows;

public partial class DecksVM: ObservableObject, IViewModel
{
    public DecksVM(IDeckRepo dr, IDeckTreeBuilder dtb, long userId)
    {
        deckTreeBuilder = dtb;
        deckRepo = dr;
        this.userId = userId;

        DeckTree = [];
    }

    #region methods
    internal async Task SyncDeckTree()
    {
        DeckTree.Clear();

        // build root-level decks without parents (ParentId == null)
        IEnumerable<DeckNode> deckTree = await
            deckTreeBuilder.BuildCountedAsync(userId);

        DeckTree.AddRange(deckTree);
    }
    #endregion

    #region public properties
    public ObservableCollection<DeckNode> DeckTree { get; init; }
    
    [ObservableProperty]
    public partial DeckNode? SelectedDeck { get; private set; }
    #endregion

    #region private things
    private readonly IDeckRepo deckRepo;
    private readonly IDeckTreeBuilder deckTreeBuilder;
    private long userId;
    #endregion
}
