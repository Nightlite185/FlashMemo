using FlashMemo.Repositories;
using FlashMemo.Services;

namespace FlashMemo.ViewModel.VMFactories;

public class FiltersVMF (DeckTreeBuilder dtb, ITagRepo tr)
{
    private readonly DeckTreeBuilder deckTreeBuilder = dtb;
    private readonly ITagRepo tagRepo = tr;
    public FiltersVM Create(long userId)
    {
        return new(
            deckTreeBuilder, tagRepo,
            userId
        );
    }
}   