using FlashMemo.Repositories;
using FlashMemo.Services;
using FlashMemo.ViewModel.Windows;

namespace FlashMemo.ViewModel.Factories;

public class EditCardVMF(ICardService cs, ITagRepo tr, ICardRepo cr, CardCtxMenuVMF ctxFactory, IDomainEventBus bus)
{
    public async Task<CardEditorVM> CreateAsync(long cardId, long userId)
    {
        CardEditorVM ecVM = new(
            cs, tr, cr, bus);

        var ccm = ctxFactory
            .Create(ecVM, userId);

        await ecVM.Initialize(cardId, ccm);

        return ecVM;
    }
}