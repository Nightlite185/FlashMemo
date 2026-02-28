using FlashMemo.Model;
using FlashMemo.Model.Persistence;
using FlashMemo.ViewModel.Bases;

namespace FlashMemo.ViewModel.Wrappers;

public class EditableCardVM: WiPCardVMBase
{
    public EditableCardVM(ICard card, ICollection<TagVM> tags)
    {
        Card = card;
        Tags = [..tags];

        
    }
    public ICard Card { get; init; }
}