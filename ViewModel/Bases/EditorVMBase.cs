using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FlashMemo.Helpers;
using FlashMemo.Model.Persistence;
using FlashMemo.Repositories;
using FlashMemo.Services;
using FlashMemo.ViewModel.Wrappers;

namespace FlashMemo.ViewModel.Bases;

public abstract partial class EditorVMBase: ObservableObject, IViewModel
{
    internal EditorVMBase(ICardService cs, ITagRepo tr, ICardRepo cr) // , long cardId || EDITOR ONLY
    {
        // this.cardId = cardId; || EDITOR ONLY
        cardService = cs;
        tagRepo = tr;
        cardRepo = cr;
    }

    #region public properties
    public CardItemVM CardVM { get; protected set; } = null!; // factory sets this by calling initialize()
    // public CardEntity LastSavedCard { get; protected set; } = null!; || EDITOR ONLY
    public ObservableCollection<TagVM> Tags { get; init; } = [];
    #endregion

    #region methods
    protected void Close() => OnCloseRequest?.Invoke();
    #endregion

    #region protected things
    protected readonly ICardService cardService;
    // protected long cardId; || EDITOR ONLY
    protected readonly ITagRepo tagRepo;
    protected readonly ICardRepo cardRepo;
    
    #endregion
    
    #region ICommands
    [RelayCommand]
    protected virtual async Task SaveChanges()
    {
        var card = CardVM.ToEntity();
        card.ReplaceTagsWith(Tags.ToEntities());

        await cardService.SaveEditedCard(card, CardAction.Modify);
    }

    [RelayCommand] // cancels changes and closes the editor grid/window
    protected virtual void CancelChanges() => Close();


    #endregion
    public event Action? OnCloseRequest;

}