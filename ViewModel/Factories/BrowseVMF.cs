using FlashMemo.Services;
using FlashMemo.ViewModel.Windows;
using Microsoft.Extensions.Logging;

namespace FlashMemo.ViewModel.Factories;

public class BrowseVMF(ICardQueryService cqs, FiltersVMF filtersVMF, 
                        CardCtxMenuVMF ctxFactory, IVMEventBus bus,
                        ILogger<BrowseVM> logger, IDialogService dialogs, SafetyNet net)
{
    public async Task<BrowseVM?> TryCreateAsync(long userId)
    {
        var res = await net.TryUnsafe(
            action: async () =>
            {
                var filtersVM = await filtersVMF
                    .CreateAsync(userId);

                BrowseVM bvm = new(
                    cqs, filtersVM,
                    userId, bus,
                    logger, dialogs, net);

                try
                {
                    var ccm = ctxFactory.Create(
                        bvm, bvm, userId);

                    await bvm.InitializeAsync(ccm);
                    return bvm;
                }

                catch
                {
                    bvm.OnClosed();
                    throw;
                }
            },
            actionTried: "open the browse view",
            retry: true);

        return res.Success ? res.Value : null;
    }
}