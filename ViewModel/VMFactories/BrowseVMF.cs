using FlashMemo.Helpers;
using FlashMemo.Repositories;
using FlashMemo.Services;
using FlashMemo.ViewModel.WindowVMs;

namespace FlashMemo.ViewModel.VMFactories;

public class BrowseVMF(
    IWindowService ws, ICardQueryService cqs,
    FiltersVMF fVMF, CardCtxMenuVMF ccmVMF)
{

    private readonly FiltersVMF filtersVMF = fVMF;
    private readonly IWindowService windowService = ws;
    private readonly ICardQueryService cardQueryS = cqs;
    private readonly CardCtxMenuVMF cardCtxMenuVMF = ccmVMF;

    public async Task<BrowseVM> CreateAsync(long userId)
    {
        var filtersVM = filtersVMF.Create(userId);

        BrowseVM bvm = new(
            windowService, cardQueryS, 
            filtersVM, userId
        );

        var ccm = cardCtxMenuVMF.Create(bvm, bvm, userId);

        await filtersVM.InitializeAsync(bvm);
        bvm.Initialize(ccm);

        //* pre-filling cards in BrowseVM with all user's cards
        var allCards = await cardQueryS.GetAllFromUser(userId);
        bvm.Cards.AddRange(allCards.ToVMs());

        return bvm;
    }
}