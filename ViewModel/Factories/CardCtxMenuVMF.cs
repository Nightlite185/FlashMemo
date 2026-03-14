using FlashMemo.Repositories;
using FlashMemo.Services;
using FlashMemo.ViewModel.Other;

namespace FlashMemo.ViewModel.Factories;

public class CardCtxMenuVMF(ICardRepo cr, ICardService cs, ManageTagsVMF mtVMF, IVMEventBus bus, DeckSelectVMF dsVMF)
{
    public CardCtxMenuVM Create(IPopupHost pph, ICtxMenuHost host, long userId)
    {
        return new(
            cs, cr, mtVMF, pph, dsVMF,
            bus, userId, host
        );
    }
}