using FlashMemo.Repositories;
using FlashMemo.Services;
using FlashMemo.ViewModel.Bases;

namespace FlashMemo.ViewModel.Windows;

public partial class CreateCardVM(ICardService cs, ITagRepo tr, ICardRepo cr)
: EditorVMBase(cs, tr, cr)
{
    
}