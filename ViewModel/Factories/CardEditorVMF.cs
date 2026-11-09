using FlashMemo.Repositories;
using FlashMemo.Services;
using FlashMemo.ViewModel.Windows;
using Microsoft.Extensions.Logging;

namespace FlashMemo.ViewModel.Factories;

public class CardEditorVMF(ICardService cardService, ICardRepo cardRepo, CardCtxMenuVMF ctxFactory, EditableCardVMF cardVMF,
                        IVMEventBus bus, DeckSelectVMF deckSelectVMF, CardTagsVMF tagsVMF,
                        ILogger<CardEditorVM> logger, IDialogService dialogs, SafetyNet net)
{
    public async Task<CardEditorVM?> TryCreateAsync(long cardId, long userId)
    {
        var res = await net.TryUnsafe(
            action: async () =>
            {
                var cardVM = await cardVMF
                    .CreateAsync(cardId);

                if (cardVM is null)
                {
                    dialogs.Show(
                        title: "Error",
                        message: "Sorry, but the card you tried to edit doesn't exist. "
                        + "This is expected if you deleted that card beforehand.",
                        buttons: DialogButtons.OK,
                        icon: DialogIcons.Error);

                    return null;
                }

                CardEditorVM editorVM = new(
                    cardService, cardRepo,
                    bus,
                    deckSelectVMF, cardVM,
                    logger, dialogs, net);

                var tagsVM = await tagsVMF
                    .CreateAsync(userId, editorVM);

                var ctxMenuVM = ctxFactory.Create(
                    editorVM, editorVM, userId);

                editorVM.Initialize(ctxMenuVM, tagsVM);
                return editorVM;
            },
            actionTried: "open the card editor",
            retry: true);

        return res.Success ? res.Value : null;
    }
}