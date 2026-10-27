using FlashMemo.Model.Persistence;
using FlashMemo.Services;
using FlashMemo.ViewModel.Popups;
using Microsoft.Extensions.Logging;

namespace FlashMemo.ViewModel.Factories;

public class DeckSelectVMF(IDeckTreeBuilder dtb, ILogger<DeckSelectVM> logger, 
                            IDialogService dialogs)
{
    private readonly IDeckTreeBuilder deckTreeBuilder = dtb;
    public async Task<DeckSelectVM> CreateAsync(Func<IDeckMeta, Task> confirm, Action cancel, long userId)
    {
        var deckTree = await deckTreeBuilder
            .BuildAsync(userId);

        return new(
            confirm, cancel, deckTree,
            logger, dialogs);
    }
}