using FlashMemo.Repositories;
using FlashMemo.Services;
using FlashMemo.ViewModel.BaseVMs;

namespace FlashMemo.ViewModel.WindowVMs;

public partial class CreateCardVM(CardService cs, TagRepo tr) : EditorVMBase(cs, tr), IViewModel
{
    
}