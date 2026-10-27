using FlashMemo.Model.Persistence;
using FlashMemo.Repositories;
using FlashMemo.Services;
using FlashMemo.ViewModel.Windows;
using Microsoft.Extensions.Logging;

namespace FlashMemo.ViewModel.Factories;

public class ReviewVMF(ICardService cs, ICardQueryService cqs, CardCtxMenuVMF ctxFactory, 
                    IDeckOptionsService deckOptService, ICardRepo cr, IVMEventBus bus,
                    IUserOptionsService uos, ILogger<ReviewVM> logger, IDialogService dialogs)
{
    public async Task<ReviewVM> CreateAsync(long userId, IDeckMeta deck)
    {
        var vm = new ReviewVM(
            cs, cqs, userId, deck,
            cr, bus, uos, deckOptService,
            logger, dialogs);

        var ctxMenu = ctxFactory
            .Create(vm, vm, userId);

        await vm.InitAsync(ctxMenu);
        return vm;
    }
}