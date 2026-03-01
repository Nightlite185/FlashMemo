using FlashMemo.Model.Persistence;
using FlashMemo.Repositories;
using FlashMemo.Services;
using FlashMemo.ViewModel.Windows;

namespace FlashMemo.ViewModel.Factories;

public class ReviewVMF(ICardService cs, ICardQueryService cqs, CardCtxMenuVMF ctxFactory, 
                        IDeckOptionsRepo deckOptRepo, ICardRepo cr, IDomainEventBus bus)
{
    public async Task<ReviewVM> CreateAsync(long userId, IDeckMeta deck)
    {
        var deckOpt = await deckOptRepo
            .GetFromDeck(deck.Id);

        var vm = new ReviewVM(
            cs, cqs, userId, 
            deck, deckOpt, cr, bus
        );

        var ctxMenu = ctxFactory.Create(vm, userId);

        await vm.InitAsync(ctxMenu);

        return vm;
    }
}