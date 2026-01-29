using FlashMemo.Repositories;
using FlashMemo.Services;
using FlashMemo.ViewModel.WindowVMs;

namespace FlashMemo.ViewModel.VMFactories;

public class BrowseVMF (
    IWindowService ws, ICardRepo cr,
    ITagRepo tr, ICardQueryService cqs,
    ICardService cs, FiltersVMF fvmf)
{
    private readonly FiltersVMF filtersVMF = fvmf;
    private readonly IWindowService windowService = ws;
    private readonly ICardRepo cardRepo = cr;
    private readonly ITagRepo tagRepo = tr;
    private readonly ICardQueryService cardQueryS = cqs;
    private readonly ICardService cardService = cs;

    public async Task<BrowseVM> CreateAsync(long userId)
    {
        var filtersVM = filtersVMF.Create(userId);

        BrowseVM bvm = new(
            windowService, cardRepo, tagRepo,
            cardQueryS,cardService, filtersVM,
            userId
        );

        await filtersVM.Initialize(bvm.ApplyFiltersAsync);

        // TODO: load the initial cards in browse VM here

        return bvm;
    }
}