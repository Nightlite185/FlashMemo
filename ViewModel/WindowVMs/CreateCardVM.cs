using FlashMemo.Repositories;
using FlashMemo.Services;
using FlashMemo.ViewModel.BaseVMs;

namespace FlashMemo.ViewModel.WindowVMs;

public partial class CreateCardVM(ICardService cs, ITagRepo tr) : EditorVMBase(cs, tr), IViewModel
{
    
}