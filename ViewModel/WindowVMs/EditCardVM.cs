using CommunityToolkit.Mvvm.Input;
using FlashMemo.Helpers;
using FlashMemo.Repositories;
using FlashMemo.Services;
using FlashMemo.ViewModel.BaseVMs;

namespace FlashMemo.ViewModel.WindowVMs;

public partial class EditCardVM(ICardService cs, ITagRepo tr): EditorVMBase(cs, tr), IViewModel
{
    #region public properties
    #endregion

    #region methods
    #endregion

    #region private things
    #endregion
    
    #region ICommands
    [RelayCommand]
    private void RevertChanges() // only reverts the vm-made changes to what the card was.
    {
        ThrowIfNotInitialized(nameof(RevertChanges));

        FrontContent = card!.FrontContent;
        BackContent = card!.BackContent ?? "";
        
        Tags.Clear();
        Tags.AddRange(card!.Tags);
    }
    #endregion
}
