using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FlashMemo.Helpers;
using FlashMemo.Model.Persistence;
using FlashMemo.Repositories;
using FlashMemo.Services;
using FlashMemo.ViewModel.WrapperVMs;

namespace FlashMemo.ViewModel.BaseVMs;

public abstract partial class EditorVMBase: ObservableObject, ICloseRequest
{
    internal EditorVMBase(ICardService cs, ITagRepo tr, ICardRepo cr, long cardId)
    {
        this.cardId = cardId;
        cardService = cs;
        tagRepo = tr;
        cardRepo = cr;
    }

    #region public properties
    public CardItemVM CardVM { get; protected set; } = null!; // factory sets this by calling initialize()
    public CardEntity LastSavedCard { get; protected set; } = null!;
    public ObservableCollection<TagVM> Tags { get; init; } = [];
    #endregion

    #region methods
    internal virtual async Task Initialize()
    {
        LastSavedCard = await cardRepo.GetCard(cardId);
        CardVM = new(LastSavedCard);

        var tags = await tagRepo.GetFromCardAsync(cardId);

        if (Tags.Count > 0) throw new InvalidOperationException(
            "Cannot initialize Tags collection because it already had elements in it.");

        Tags.AddRange(tags.ToVMs());
    }

    #endregion

    #region protected things
    protected readonly ICardService cardService;
    protected long cardId;
    protected readonly ITagRepo tagRepo;
    protected readonly ICardRepo cardRepo;
    protected long userId;
    #endregion
    
    #region ICommands
    [RelayCommand]
    protected virtual async Task SaveChanges()
    {
        var card = CardVM.ToEntity();
        card.ReplaceTagsWith(Tags.ToEntities());

        await cardService.SaveEditedCard(card, CardAction.Modify);

        OnCloseRequest?.Invoke();
    }

    [RelayCommand] // cancels changes and closes the editor grid/window
    protected virtual void CancelChanges() => OnCloseRequest?.Invoke();

    #endregion
    public event Action? OnCloseRequest;
}