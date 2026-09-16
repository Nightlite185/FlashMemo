using FlashMemo.Repositories;
using FlashMemo.Services;
using FlashMemo.ViewModel.Other;

namespace FlashMemo.ViewModel.Factories;

public class CardCtxMenuVMF(ICardRepo cr, ICardService cs, IVMEventBus bus,
                            DeckSelectVMF dsVMF, IUserOptionsService userOptionsService)
{
    public CardCtxMenuVM Create(IPopupHost pph, ICtxMenuHost host, long userId)
    {
        return new(
            cs, cr, pph, dsVMF,
            bus, userOptionsService,
            userId, host
        );
    }
}
