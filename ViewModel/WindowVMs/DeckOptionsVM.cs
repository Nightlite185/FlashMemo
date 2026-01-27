using FlashMemo.Model.Persistence;
using FlashMemo.ViewModel.BaseVMs;

namespace FlashMemo.ViewModel.WindowVMs;

public partial class DeckOptionsVM: OptionsVMBase<DeckOptions>, IViewModel
{
    protected override DeckOptions ToSnapshot()
    {
        throw new NotImplementedException();
    }
}
