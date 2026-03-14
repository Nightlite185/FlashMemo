using CommunityToolkit.Mvvm.ComponentModel;
using FlashMemo.Helpers;
using FlashMemo.Model.Persistence;
using FlashMemo.Repositories;
using FlashMemo.Services;
using FlashMemo.ViewModel.Wrappers;

namespace FlashMemo.ViewModel.Other;

public partial class CardTagsVM(ITagRepo tr, IVMEventBus bus, long userId): ObservableObject, ICardTagsVM, IViewModel
{
    #region public properties
    public IEnumerable<TagVM> CardTags => cardTags;
    public IEnumerable<TagVM> AllTags => allTags;
    #endregion

    #region methods
    internal async Task InitAsync(long? cardId, ICardTagsVMHost host)
    {
        if (cardId is null) 
            throw new NotImplementedException();

        this.host = host;

        var allTagVMs = (await tagRepo
            .GetFromUser(userId))
            .ToVMs();

        var cardTagVMs = (await tagRepo
            .GetFromCard((long)cardId))
            .ToVMs();

        allTags.Clear();
        cardTags.Clear();

        allTags.AddRange(allTagVMs);
        cardTags.AddRange(cardTagVMs);
    }
    public async Task<TagVM?> AddTagAsync(string tagName)
    {
        if (allTags.Any(t => t.Name == tagName))
            return null;

        var tag = Tag.CreateNew(tagName);
        await tagRepo.CreateNew(tag);

        eventBus.NotifyDomain();
        return new(tag);
    }
    public bool RemoveTag(long tagId)
    {
        int removed = allTags.RemoveAll(
            t => t.Id == tagId);
        
        if (removed > 1) throw new InvalidOperationException(
            "There were more than one tag with same id, detected while removing.");

        return removed == 1;
    }
    #endregion

    #region private things
    private ICardTagsVMHost host = null!;
    private readonly List<TagVM> allTags = [];
    private readonly List<TagVM> cardTags = [];
    private readonly ITagRepo tagRepo = tr;
    private readonly IVMEventBus eventBus = bus;
    private readonly long userId = userId;
    private long cardId;
    #endregion
}