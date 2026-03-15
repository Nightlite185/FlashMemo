using FlashMemo.Repositories;
using FlashMemo.Services;
using FlashMemo.ViewModel.Other;

namespace FlashMemo.ViewModel.Factories;

public class CardTagsVMF(ITagRepo tr, IVMEventBus bus)
{
    public async Task<CardTagsVM> CreateAsync(long userId, ICardTagsVMHost host, ITagsSource source)
    {
        CardTagsVM vm = new(
            tr, bus, userId);

        await vm.InitAsync(host, source);
        return vm;
    }
}