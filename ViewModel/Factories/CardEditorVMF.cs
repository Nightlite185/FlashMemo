using FlashMemo.Repositories;
using FlashMemo.Services;
using FlashMemo.ViewModel.Windows;

namespace FlashMemo.ViewModel.Factories;

public class CardEditorVMF(ICardService cardService, ICardRepo cardRepo, CardCtxMenuVMF ctxFactory, EditableCardVMF cardVMF,
                        IVMEventBus bus, IDeckRepo deckRepo, DeckSelectVMF deckSelectVMF, CardTagsVMF tagsVMF)
{
    public async Task<CardEditorVM?> CreateAsync(long cardId, long userId)
    {
        var cardVM = await cardVMF
            .CreateAsync(cardId);

        if (cardVM is null) return null;

        CardEditorVM editorVM = new(
            cardService, cardRepo, 
            bus, deckRepo, 
            deckSelectVMF, cardVM);

        var tagsVM = await tagsVMF
            .CreateAsync(userId, editorVM);

        var ctxMenuVM = ctxFactory.Create(
            editorVM, editorVM, userId);

        editorVM.Initialize(ctxMenuVM, tagsVM);
        return editorVM;
    }
}