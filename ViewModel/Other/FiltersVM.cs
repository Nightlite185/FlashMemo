using System.Collections.Immutable;
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FlashMemo.Helpers;
using FlashMemo.Model;
using FlashMemo.Model.Domain;
using FlashMemo.Repositories;
using FlashMemo.Services;
using FlashMemo.ViewModel.Bases;
using FlashMemo.ViewModel.Wrappers;

namespace FlashMemo.ViewModel.Other;

public sealed partial class FiltersVM(IDeckTreeBuilder deckTB, ITagRepo tagRepo, IVMEventBus bus, 
                                        ILastSessionService lastSession, long userId): BaseVM(bus)
{
    #region public properties
    public Filters CachedFilters { get; private set; } = null!;

    public ObservableCollection<TagVM> Tags { get; set; } = [];
    public ImmutableList<CardStateVM> States { get; set; } =
        [..Enum.GetValues<CardState>().ToVMs()];
    public ObservableCollection<DeckNode> DeckTree { get; set; } = [];

    [ObservableProperty] public partial bool? IsBuried { get; set; }
    [ObservableProperty] public partial bool? IsSuspended { get; set; }
    [ObservableProperty] public partial bool? IsDue { get; set; }
    [ObservableProperty] public partial bool IncludeChildrenDecks { get; set; } 
    //* whether to recursively include all children of the chosen deck

    [ObservableProperty] public partial int? IntervalDays { get; set; }
    [ObservableProperty] public partial DateTime? Due { get; set; }
    [ObservableProperty] public partial DateTime? LastReviewed { get; set; }
    [ObservableProperty] public partial DateTime? LastModified { get; set; }
    [ObservableProperty] public partial DateTime? Created { get; set; }
    //? everywhere with datetime I can do like int input box with 0 meaning today,
    //? -1 yesterday, and 1 meaning tmrw, Instead of some fancy datetime picker.

    [ObservableProperty] public partial int? OverdueByDays { get; set; }
    //* null => not chosen, 0 => due today, 1 => overdue by 1 day.
    #endregion

    #region methods
    [RelayCommand]
    private void ResetFilters()
    {
        IsBuried = null;
        IsSuspended = null;
        IsDue = null;
        IncludeChildrenDecks = true;
        IntervalDays = null;
        Due = null;
        LastReviewed = null;
        LastModified = null;
        Created = null;
        OverdueByDays = null;

        foreach (var s in States)
            s.IsSelected = false;

        foreach (var t in Tags)
            t.IsSelected = false;

        foreach (var d in DeckTree.Flatten())
            d.IsSelected = false;
    }

    public Filters TakeSnapshot()
    {
        var snapshot = new Filters()
        {
            UserId = userId,
            
            TagIds = [..OnlySelectedTags()],
            States = [..OnlySelectedStates()],
            DeckIds = [..GetDeckIds()],

            IntervalDays = (IntervalDays > 0)
                ? IntervalDays
                : null,

            IncludeChildrenDecks = IncludeChildrenDecks,
            OverdueByDays = OverdueByDays,
            LastModified = LastModified,
            LastReviewed = LastReviewed,
            IsSuspended = IsSuspended,
            IsBuried = IsBuried,
            Created = Created,
            IsDue = IsDue,
            Due = Due
        };

        CachedFilters = snapshot;
        lastSession.LastFilters = snapshot;

        return snapshot;
    }
    internal async Task InitializeAsync()
    {
        eventBus.DomainChanged += OnDomainChanged;

        CachedFilters = lastSession.LastFilters
            ?? Filters.GetEmpty(userId);

        //* mapping scalars from last filters
        IncludeChildrenDecks = CachedFilters.IncludeChildrenDecks;

        IsSuspended   = CachedFilters.IsSuspended;
        IsBuried      = CachedFilters.IsBuried;
        IsDue         = CachedFilters.IsDue;

        Due           = CachedFilters.Due;
        Created       = CachedFilters.Created;
        IntervalDays  = CachedFilters.IntervalDays;
        LastReviewed  = CachedFilters.LastReviewed;
        LastModified  = CachedFilters.LastModified;
        OverdueByDays = CachedFilters.OverdueByDays;

        await ReloadDomainAsync();
        MarkVMsSelected(CachedFilters);
    }

    //* only reloads those filters that are dynamically dependent on the current domain.
    protected override async Task ReloadDomainAsync()
    {
        //* snapshotting selected ids
        long[] selectedDecks = [..OnlySelectedDecks()];
        long[] selectedTags = [..OnlySelectedTags()];

        DeckTree.Clear();
        Tags.Clear();

        DeckTree.AddRange(await deckTB
            .BuildAsync(userId));

        Tags.AddRange((await tagRepo
            .GetFromUser(userId))
            .ToVMs());

        //* marking all tags and decks that still exist and were selected before the reload.
        DeckTree.Flatten()
            .IntersectBy(selectedDecks, d => d.Id)
            .ForEach(d => d.IsSelected = true);

        Tags.IntersectBy(selectedTags, t => t.Id)
            .ForEach(t => t.IsSelected = true);
    }

    private void MarkVMsSelected(Filters filters)
    {
        //* marking decks, tags and states from previous filters as selected
        //* (those that still exist, at least)
        DeckTree.Flatten()
            .IntersectBy(filters.DeckIds, d => d.Id)
            .ForEach(d => d.IsSelected = true);

        Tags.IntersectBy(filters.TagIds, t => t.Id)
            .ForEach(t => t.IsSelected = true);

        States.IntersectBy(filters.States, s => s.State)
            .ForEach(s => s.IsSelected = true);
    }
    private IEnumerable<long> GetDeckIds()
    {
        var selectedDecks = DeckTree
            .Flatten()
            .Where(d => d.IsSelected);
            
        return IncludeChildrenDecks
            ? selectedDecks
                .Flatten()     // we take all children of only the selected nodes, by flattening them again
                .Select(d => d.Id)

            : selectedDecks
                .Select(d => d.Id); // else we take only the selected nodes
    }
    private IEnumerable<long> OnlySelectedDecks()
    {
        return DeckTree
            .Flatten()
            .Where(d => d.IsSelected)
            .Select(d => d.Id);
    }
    private IEnumerable<long> OnlySelectedTags()
    {
        return Tags
            .Where(t => t.IsSelected)
            .Select(t => t.Id);
    }
    private IEnumerable<CardState> OnlySelectedStates()
    {
        return States
            .Where(vm => vm.IsSelected)
            .Select(vm => vm.State);
    }
    #endregion
}