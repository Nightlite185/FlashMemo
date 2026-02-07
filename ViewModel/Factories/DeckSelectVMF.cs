using FlashMemo.Model.Persistence;
using FlashMemo.Services;
using FlashMemo.ViewModel.Popups;
using FlashMemo.ViewModel.Wrappers;

namespace FlashMemo.ViewModel.Factories;

public class DeckSelectVMF(IDeckTreeBuilder dtb)
{
    private readonly IDeckTreeBuilder deckTreeBuilder = dtb;
    public async Task<DeckSelectVM> CreateAsync(Func<DeckNode, Task> confirm, Action cancel, long userId)
    {
        var deckTree = await deckTreeBuilder
            .BuildAsync(userId);

        return new(
            confirm, cancel, deckTree);
    }
}