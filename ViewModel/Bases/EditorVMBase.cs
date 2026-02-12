using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FlashMemo.Repositories;
using FlashMemo.Services;

namespace FlashMemo.ViewModel.Bases;

[Obsolete("useless")]
public abstract partial class EditorVMBase: ObservableObject, IViewModel
{
    internal EditorVMBase(ICardService cs, ITagRepo tr, ICardRepo cr)
    {
        cardService = cs;
        tagRepo = tr;
        cardRepo = cr;
    }

    #region methods
    protected void Close() => OnCloseRequest?.Invoke();
    #endregion

    #region protected things
    protected readonly ICardService cardService;
    protected readonly ITagRepo tagRepo;
    protected readonly ICardRepo cardRepo;
    
    #endregion
    
    #region ICommands
    
    [RelayCommand] // cancels changes and closes the editor grid/window
    protected virtual void CancelChanges() => Close();


    #endregion
    public event Action? OnCloseRequest;

}