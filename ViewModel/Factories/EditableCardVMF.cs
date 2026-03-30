using FlashMemo.Repositories;
using FlashMemo.Helpers;
using FlashMemo.ViewModel.Wrappers;
using FlashMemo.Services;

namespace FlashMemo.ViewModel.Factories;

public class EditableCardVMF(ICardRepo cardRepo, ITagRepo tagRepo, INoteComparer noteComparer)
{
    public async Task<EditableCardVM?> CreateAsync(long cardId)
    {
        var card = await cardRepo
            .GetById(cardId);

        // if card doesnt exist -> we return null and let caller deal with it.
        if (card is null) return null;

        var tags = await tagRepo
            .GetFromCard(cardId);

        card.Tags.AddRange(tags); // snapshotting old tags

        return new(card, noteComparer);
    }
}