using FlashMemo.Repositories;
using FlashMemo.Services;
using FlashMemo.ViewModel.Windows;

namespace FlashMemo.ViewModel.Factories;

public class EditCardVMF(ICardService cs, ITagRepo tr, ICardRepo cr)
{
    private readonly ICardService cardService = cs;
    private readonly ITagRepo tagRepo = tr;
    private readonly ICardRepo cardRepo = cr;

    public async Task<EditCardVM> CreateAsync(long cardId)
    {
        EditCardVM ecVM = new(
            cardService, tagRepo, cardRepo);

        await ecVM.Initialize(cardId);

        return ecVM;
    }
}