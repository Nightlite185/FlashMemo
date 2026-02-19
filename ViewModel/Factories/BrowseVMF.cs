using FlashMemo.Helpers;
using FlashMemo.Services;
using FlashMemo.ViewModel.Windows;

namespace FlashMemo.ViewModel.Factories;

public class BrowseVMF(ICardQueryService cqs,
    FiltersVMF fVMF, CardCtxMenuVMF ccmVMF)
{
    private readonly FiltersVMF filtersVMF = fVMF;
    private readonly ICardQueryService cardQueryS = cqs;
    private readonly CardCtxMenuVMF cardCtxMenuVMF = ccmVMF;

    public async Task<BrowseVM> CreateAsync(long userId)
    {
        var filtersVM = filtersVMF.Create(userId);

        BrowseVM bvm = new(
            cardQueryS, filtersVM,
            userId
        );

        var ccm = cardCtxMenuVMF.Create(bvm, userId);

        await filtersVM.InitializeAsync(bvm);
        bvm.Initialize(ccm);

        //* pre-filling cards in BrowseVM with all user's cards
        var allCards = await cardQueryS.GetAllFromUser(userId);
        bvm.Cards.AddRange(allCards.ToVMs());

        return bvm;
    }
}