using FlashMemo.Repositories;
using FlashMemo.Services;
using FlashMemo.ViewModel.Windows;

namespace FlashMemo.ViewModel.Factories;

public class ReviewVMF(ICardService cs, ICardQueryService cqs, CardCtxMenuVMF ctx, IDeckOptionsRepo dor)
{
    private readonly ICardService cardService = cs;
    private readonly ICardQueryService cardQuery = cqs;
    private readonly CardCtxMenuVMF ctxMenuVMF = ctx;
    private readonly IDeckOptionsRepo deckOptRepo = dor;

    public async Task<ReviewVM> CreateAsync(long userId, long deckId)
    {
        var deckOpt = await deckOptRepo.GetFromDeck(deckId);

        var vm = new ReviewVM(
            cardService, cardQuery, 
            userId, deckId, deckOpt);

        var ctxMenu = ctxMenuVMF
            .Create(vm, vm, userId);

        await vm.InitAsync(ctxMenu);

        return vm;
    }
}