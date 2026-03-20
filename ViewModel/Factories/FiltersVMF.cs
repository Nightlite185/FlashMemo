using FlashMemo.Repositories;
using FlashMemo.Services;
using FlashMemo.ViewModel.Other;

namespace FlashMemo.ViewModel.Factories;

public class FiltersVMF (IDeckTreeBuilder dtb, ITagRepo tr, IVMEventBus eventBus, ILastSessionService lastSession)
{
    private readonly IDeckTreeBuilder deckTreeBuilder = dtb;
    private readonly ITagRepo tagRepo = tr;
    public async Task<FiltersVM> CreateAsync(long userId)
    {
        var vm = new FiltersVM(
            deckTreeBuilder, tagRepo,
            eventBus, lastSession, userId);

        await vm.InitializeAsync();
        return vm;
    }
}   