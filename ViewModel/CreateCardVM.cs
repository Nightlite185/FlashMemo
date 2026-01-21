using FlashMemo.Repositories;
using FlashMemo.Services;

namespace FlashMemo.ViewModel
{
    public partial class CreateCardVM(CardService cs, TagRepo tr) : EditorVMBase(cs, tr), IViewModel
    {
        
    }
}