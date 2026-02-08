using FlashMemo.Services;
using FlashMemo.ViewModel.Windows;

namespace FlashMemo.ViewModel.Factories;

public class ReviewVMF(ICardService cs, ICardQueryService cqs)
{
    private readonly ICardService cardService = cs;
    private readonly ICardQueryService cardQuery = cqs;

    public async Task<ReviewVM> CreateAsync(long userId, long deckId)
    {
        var vm = new ReviewVM(
            cardService, cardQuery, 
            userId, deckId);

        await vm.InitAsync();

        return vm;
    }
}