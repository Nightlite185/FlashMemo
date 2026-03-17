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
    public IEnumerable<TagVM> CardTags => host.Tags;
    public IEnumerable<TagVM> AllTags => allTags;
    #endregion

    #region methods
    internal async Task InitAsync(ICardTagsVMHost host)
    {
        this.host = host;

        var allTagVMs = (await tagRepo
            .GetFromUser(userId))
            .ToVMs();

        allTags.Clear();
        allTags.AddRange(allTagVMs);
    }
    public async Task<TagVM?> AddTagAsync(string tagName)
    {
        static bool stringEquals(string a, string b) => 
            string.Equals(a, b, StringComparison.OrdinalIgnoreCase);

        if (host.Tags.Any(t => stringEquals(t.Name, tagName)))
            return null;

        if (allTags.SingleOrDefault(t => 
            stringEquals(t.Name, tagName)) is TagVM exists)
        {
            host.Tags.Add(exists);
            eventBus.NotifyDomain();

            return exists;
        }

        var tag = Tag.CreateNew(tagName, userId);
        TagVM vm = new(tag);

        await tagRepo.CreateNew(tag);
        allTags.Add(vm);
        host.Tags.Add(vm);

        eventBus.NotifyDomain();
        return vm;
    }
    public bool RemoveTag(long tagId)
    {
        int removed = host.Tags.RemoveAll(
            t => t.Id == tagId);
        
        if (removed > 1) throw new InvalidOperationException(
            "There were more than one tag with same id, detected while removing.");

        return removed == 1;
    }
    #endregion

    #region private things
    private ICardTagsVMHost host = null!;
    private readonly List<TagVM> allTags = [];
    private readonly ITagRepo tagRepo = tr;
    private readonly IVMEventBus eventBus = bus;
    private readonly long userId = userId;
    #endregion
}