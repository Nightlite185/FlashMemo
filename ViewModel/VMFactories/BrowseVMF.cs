using FlashMemo.Helpers;
using FlashMemo.Repositories;
using FlashMemo.Services;
using FlashMemo.ViewModel.WindowVMs;

namespace FlashMemo.ViewModel.VMFactories;

public class BrowseVMF(
    IWindowService ws, ICardRepo cr, ICardQueryService cqs,
    ICardService cs, FiltersVMF fVMF, ManageTagsVMF mtVMF)
{
    private readonly FiltersVMF filtersVMF = fVMF;
    private readonly ManageTagsVMF manageTagsVMF = mtVMF;
    private readonly IWindowService windowService = ws;
    private readonly ICardRepo cardRepo = cr;
    private readonly ICardQueryService cardQueryS = cqs;
    private readonly ICardService cardService = cs;

    public async Task<BrowseVM> CreateAsync(long userId)
    {
        var filtersVM = filtersVMF.Create(userId);

        BrowseVM bvm = new(
            windowService, cardRepo, manageTagsVMF,
            cardQueryS, cardService, filtersVM,
            userId
        );

        await filtersVM.Initialize(bvm.ApplyFiltersAsync);

        //* pre-filling cards in BrowseVM with all user's cards
        var allCards = await cardQueryS.GetAllFromUser(userId);
        bvm.Cards.AddRange(allCards.ToVMs());

        return bvm;
    }
}