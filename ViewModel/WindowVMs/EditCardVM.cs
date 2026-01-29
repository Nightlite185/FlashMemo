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
        CardVM.FrontContent = LastSavedCard.FrontContent;
        CardVM.BackContent = LastSavedCard.BackContent ?? "";

        Tags.Clear();
        Tags.AddRange(card.Tags); // TODO: figure this out, if user edited tags in ManageTagsVM,
                                  //todo LastSavedCard.Tags is obsolete -> load new tags for old ids from tag repo.
    }                             //! btw wtf am i even doing, this is supposed to be an abstraction of editor
    #endregion                    //! but Im implementing features for both creator and editor, its abstracted cleanly.
}                                 // TODO: refactor this to actually make sense. ^^^
