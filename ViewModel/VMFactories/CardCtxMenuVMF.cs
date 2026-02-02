using FlashMemo.Repositories;
using FlashMemo.Services;

namespace FlashMemo.ViewModel.VMFactories;

public class CardCtxMenuVMF(ICardRepo cr, ICardService cs, ManageTagsVMF mtVMF)
{
    private readonly ManageTagsVMF manageTagsVMF = mtVMF;
    private readonly ICardRepo cardRepo = cr;
    private readonly ICardService cardService = cs;

    public CardCtxMenuVM Create(IPopupHost pph, IReloadHandler rh, long userId)
    {
        return new(
            cardService, cardRepo, 
            manageTagsVMF, 
            pph, rh, userId);
    }
}