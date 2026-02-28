using FlashMemo.Repositories;
using FlashMemo.Services;
using FlashMemo.ViewModel.Windows;

namespace FlashMemo.ViewModel.Factories;

public class EditCardVMF(ICardService cs, ITagRepo tr, ICardRepo cr, CardCtxMenuVMF ccmVMF)
{
    private readonly ICardService cardService = cs;
    private readonly ITagRepo tagRepo = tr;
    private readonly ICardRepo cardRepo = cr;
    private readonly CardCtxMenuVMF cardCtxMenuVMF = ccmVMF;

    public async Task<CardEditorVM> CreateAsync(long cardId, long userId)
    {
        CardEditorVM ecVM = new(
            cardService, tagRepo, cardRepo);

        var ccm = cardCtxMenuVMF
            .Create(ecVM, userId);

        await ecVM.Initialize(cardId, ccm);

        return ecVM;
    }
}