using FlashMemo.Model.Persistence;
using FlashMemo.Repositories;
using FlashMemo.ViewModel.Popups;

namespace FlashMemo.ViewModel.Factories;

public class ManageTagsVMF(ITagRepo tr)
{
    private readonly ITagRepo tagRepo = tr;

    public async Task<ManageTagsVM> CreateAsync(Func<IEnumerable<Tag>, bool, Task> confirm, 
        Action cancel, long cardId, long userId)
    {
        ManageTagsVM vm = new(
            confirm, cancel, tagRepo,
            cardId, userId
        );

        await vm.InitializeAsync(userId, cardId);
        return vm;
    }
}