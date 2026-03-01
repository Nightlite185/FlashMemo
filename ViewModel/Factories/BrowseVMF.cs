using FlashMemo.Helpers;
using FlashMemo.Services;
using FlashMemo.ViewModel.Windows;

namespace FlashMemo.ViewModel.Factories;

public class BrowseVMF(ICardQueryService cqs, FiltersVMF filtersVMF, 
                    CardCtxMenuVMF ctxFactory, IDomainEventBus bus)
{
    public async Task<BrowseVM> CreateAsync(long userId)
    {
        var filtersVM = filtersVMF.Create(userId);

        BrowseVM bvm = new(
            cqs, filtersVM,
            userId, bus
        );

        var ccm = ctxFactory.Create(bvm, userId);

        await filtersVM.InitializeAsync(bvm);
        bvm.Initialize(ccm);

        //* pre-filling cards in BrowseVM with all user's cards
        var allCards = await cqs.GetAllFromUser(userId);
        bvm.Cards.AddRange(allCards.ToVMs());

        return bvm;
    }
}