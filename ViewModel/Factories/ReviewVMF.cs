using FlashMemo.Model.Persistence;
using FlashMemo.Repositories;
using FlashMemo.Services;
using FlashMemo.ViewModel.Windows;
using Microsoft.Extensions.Logging;

namespace FlashMemo.ViewModel.Factories;

public class ReviewVMF(ICardService cs, ICardQueryService cqs, CardCtxMenuVMF ctxFactory, 
                    IDeckOptionsService deckOptService, ICardRepo cr, IVMEventBus bus,
                    IUserOptionsService uos, ILogger<ReviewVM> logger, IDialogService dialogs, SafetyNet net)
{
    public async Task<ReviewVM?> TryCreateAsync(long userId, IDeckMeta deck)
    {
        var res = await net.TryUnsafe(
            action: async () =>
            {
                var vm = new ReviewVM(
                    cs, cqs, userId, deck,
                    cr, bus, uos, deckOptService,
                    logger, dialogs, net);

                var ctxMenu = ctxFactory
                    .Create(vm, vm, userId);

                try { await vm.InitAsync(ctxMenu); }
                catch
                {
                    vm.OnClosed();
                    throw;
                }
                return vm;
            },
            actionTried: "open the review session",
            retry: true);

        return res.Success ? res.Value : null;
    }
}