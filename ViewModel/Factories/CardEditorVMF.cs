using FlashMemo.Repositories;
using FlashMemo.Services;
using FlashMemo.ViewModel.Windows;

namespace FlashMemo.ViewModel.Factories;

public class CardEditorVMF(ICardService cs, ITagRepo tr, ICardRepo cr, 
                        CardCtxMenuVMF ctxFactory, IDomainEventBus bus, 
                        IDeckRepo dr, DeckSelectVMF dsVMF)
{
    public async Task<CardEditorVM> CreateAsync(long cardId, long userId)
    {
        CardEditorVM ecVM = new(
            cs, tr, cr, 
            bus, dr, dsVMF);

        var ccm = ctxFactory
            .Create(ecVM, ecVM, userId);

        await ecVM.Initialize(cardId, ccm);

        return ecVM;
    }
}