using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FlashMemo.Helpers;
using FlashMemo.Model.Persistence;
using FlashMemo.Repositories;
using FlashMemo.Services;

namespace FlashMemo.ViewModel.BaseVMs;

public abstract partial class EditorVMBase: ObservableObject, ICloseRequest
{
    public EditorVMBase(ICardService cs, ITagRepo tr)
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
    public virtual async Task Initialize(CardEntity card, long currentUserId)
    {
        this.card = card;
        this.userId = currentUserId;
        
        Tags.Clear(); // clearing for safety, just in case.
        Tags.AddRange(card.Tags);

        AllTags.Clear(); // here too

        AllTags.AddRange(await tagRepo
            .GetFromUserAsync(currentUserId));
    }
    protected virtual void ThrowIfNotInitialized(string calledMember)
    {
        if (!Initialized) throw new InvalidOperationException(
            $"Tried to call {nameof(calledMember)} without calling {nameof(Initialize)}() first"
        );
    }
    #endregion

    #region protected things
    protected readonly ICardService cardService;
    
    [ObservableProperty]
    protected partial CardEntity? card { get; set; }

    protected bool Initialized 
        => card is not null && userId is not null;
    protected readonly ITagRepo tagRepo;
    protected long? userId;
    #endregion
    
    #region ICommands
    [RelayCommand]
    protected virtual async Task SaveChanges()
    {
        ThrowIfNotInitialized(nameof(SaveChanges));

        card!.Tags = [..Tags];
        card!.FrontContent = FrontContent;
        card!.BackContent = BackContent;

        await cardService.SaveEditedCard(card, CardAction.Modify);

        OnCloseRequest?.Invoke();
    }

    [RelayCommand]
    protected virtual void CancelChanges() // cancels changes and closes the editor grid/window
    {
        ThrowIfNotInitialized(nameof(CancelChanges));
        OnCloseRequest?.Invoke();
    }
    #endregion
    public event Action? OnCloseRequest;
}