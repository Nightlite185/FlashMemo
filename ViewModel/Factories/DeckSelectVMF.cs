using FlashMemo.Model.Persistence;
using FlashMemo.Services;
using FlashMemo.ViewModel.Popups;
using Microsoft.Extensions.Logging;

namespace FlashMemo.ViewModel.Factories;

public class DeckSelectVMF(IDeckTreeBuilder dtb, ILogger<DeckSelectVM> logger, 
                            IDialogService dialogs, SafetyNet net)
{
    private readonly IDeckTreeBuilder deckTreeBuilder = dtb;
    public async Task<DeckSelectVM?> TryCreateAsync(Func<IDeckMeta, Task> confirm, Action cancel, long userId)
    {
        var res = await net.TryUnsafe(
            action: async () =>
            {
                var deckTree = await deckTreeBuilder
                    .BuildAsync(userId);

                return new DeckSelectVM(
                    confirm, cancel, deckTree,
                    logger, dialogs, net);
            },
            actionTried: "load the available decks",
            retry: true);

        return res.Success ? res.Value : null;
    }
}