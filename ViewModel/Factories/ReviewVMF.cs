using FlashMemo.Model.Persistence;
using FlashMemo.Repositories;
using FlashMemo.Services;
using FlashMemo.ViewModel.Windows;

namespace FlashMemo.ViewModel.Factories;

public class ReviewVMF(ICardService cs, ICardQueryService cqs, CardCtxMenuVMF ctxFactory, 
                        IDeckOptionsService deckOptRepo, ICardRepo cr, IVMEventBus bus, IUserOptionsService uos)
{
    public async Task<ReviewVM> CreateAsync(long userId, IDeckMeta deck)
    {
        var deckOpt = await deckOptRepo
            .GetFromDeck(deck.Id);

        var vm = new ReviewVM(
            cs, cqs, userId, deck, 
            deckOpt, cr, bus, uos
        );

        var ctxMenu = ctxFactory.Create(vm, vm, userId);

        await vm.InitAsync(ctxMenu);

        return vm;
    }
}