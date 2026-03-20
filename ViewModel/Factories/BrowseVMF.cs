using FlashMemo.Helpers;
using FlashMemo.Services;
using FlashMemo.ViewModel.Windows;

namespace FlashMemo.ViewModel.Factories;

public class BrowseVMF(ICardQueryService cqs, FiltersVMF filtersVMF, 
                    CardCtxMenuVMF ctxFactory, IVMEventBus bus)
{
    public async Task<BrowseVM> CreateAsync(long userId)
    {
        var filtersVM = await filtersVMF
            .CreateAsync(userId);

        BrowseVM bvm = new(
            cqs, filtersVM,
            userId, bus);

        var ccm = ctxFactory.Create(
            bvm, bvm, userId);

        await bvm.InitializeAsync(ccm);

        return bvm;
    }
}