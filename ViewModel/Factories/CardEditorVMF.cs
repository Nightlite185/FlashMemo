using FlashMemo.Repositories;
using FlashMemo.Services;
using FlashMemo.ViewModel.Windows;

namespace FlashMemo.ViewModel.Factories;

public class CardEditorVMF(ICardService cs, ITagRepo tr, ICardRepo cr, CardCtxMenuVMF ctxFactory,
                        IVMEventBus bus, IDeckRepo dr, DeckSelectVMF dsVMF, CardTagsVMF cardTagsVMF)
{
    public async Task<CardEditorVM> CreateAsync(long cardId, long userId)
    {
        CardEditorVM editorVM = new(
            cs, tr, cr, bus,
            dr, dsVMF);

        var tagsVM = await cardTagsVMF.CreateAsync(
            userId, editorVM);

        var ctxMenuVM = ctxFactory.Create(
            editorVM, editorVM, userId);

        await editorVM.Initialize(cardId, ctxMenuVM, tagsVM);
        return editorVM;
    }
}