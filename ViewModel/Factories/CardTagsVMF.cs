using FlashMemo.Repositories;
using FlashMemo.Services;
using FlashMemo.ViewModel.Other;
using Microsoft.Extensions.Logging;

namespace FlashMemo.ViewModel.Factories;

public class CardTagsVMF(ITagRepo tr, IVMEventBus bus,
                        ILogger<CardTagsVM> logger, IDialogService dialogs)
{
    public async Task<CardTagsVM> CreateAsync(long userId, ICardTagsVMHost host)
    {
        CardTagsVM vm = new(
            tr, bus, userId,
            logger, dialogs);

        await vm.InitAsync(host);
        return vm;
    }
}