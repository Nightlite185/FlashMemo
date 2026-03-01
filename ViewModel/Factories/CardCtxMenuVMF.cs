using FlashMemo.Repositories;
using FlashMemo.Services;

namespace FlashMemo.ViewModel.Factories;

public class CardCtxMenuVMF(ICardRepo cr, ICardService cs, ManageTagsVMF mtVMF, IDomainEventBus bus, DeckSelectVMF dsVMF)
{
    public CardCtxMenuVM Create(IPopupHost pph, long userId)
    {
        return new(
            cs, cr, mtVMF, pph, 
            dsVMF, bus, userId
        );
    }
}