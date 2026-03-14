using FlashMemo.Repositories;
using FlashMemo.Services;
using FlashMemo.ViewModel.Other;

namespace FlashMemo.ViewModel.Factories;

public class CardTagsVMF(ITagRepo tr, IVMEventBus bus)
{
    public async Task<CardTagsVM> CreateAsync(long userId, long? cardId, ICardTagsVMHost host)
    {
        CardTagsVM vm = new(
            tr, bus, userId);

        await vm.InitAsync(cardId, host);
        return vm;
    }
}