using FlashMemo.Repositories;
using FlashMemo.Services;

namespace FlashMemo.ViewModel.Factories;

public class CardCtxMenuVMF(ICardRepo cr, ICardService cs, ManageTagsVMF mtVMF, DeckSelectVMF dsVMF)
{
    private readonly ManageTagsVMF manageTagsVMF = mtVMF;
    private readonly ICardRepo cardRepo = cr;
    private readonly ICardService cardService = cs;
    private readonly DeckSelectVMF deckSelectVMF = dsVMF;

    public CardCtxMenuVM Create(IPopupHost pph, IReloadHandler rh, long userId)
    {
        return new(
            cardService, cardRepo, 
            manageTagsVMF, pph, rh, 
            deckSelectVMF, userId);
    }
}