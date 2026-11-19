using CommunityToolkit.Mvvm.ComponentModel;
using FlashMemo.Helpers;
using FlashMemo.Model.Persistence;
using FlashMemo.Repositories;
using FlashMemo.Services;
using FlashMemo.ViewModel.Wrappers;

namespace FlashMemo.ViewModel.Other;

public partial class CardTagsVM(ITagRepo tr, IVMEventBus bus, long userId, SafetyNet net)
    : ObservableObject, ICardTagsVM, IViewModel
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

    // Returns the selected tag (including an already selected one), or null on failure/no submission.
    public async Task<TagVM?> TryAddTagAsync(string tagName)
    {
        tagName = tagName.Trim();
        if (isSavingTag || string.IsNullOrWhiteSpace(tagName))
            return null;

        static bool sameName(string a, string b)
            => string.Equals(a, b, StringComparison.OrdinalIgnoreCase);

        if (host.Tags.FirstOrDefault(t => sameName(t.Name, tagName)) is TagVM selected)
            return selected;

        isSavingTag = true;

        try
        {
            var tagVM = allTags.SingleOrDefault(t => sameName(t.Name, tagName));

            if (tagVM is null)
            {
                var result = await net.TryUnsafe(
                    () => tagRepo.GetOrCreate(Tag.CreateNew(tagName, userId)),
                    actionTried: "add the tag. Your current tags and entered text have been kept; you can try again.",
                    retry: false);

                if (!result.Success)
                    return null;

                tagVM = new TagVM(result.Value);
                allTags.Add(tagVM);
            }

            host.Tags.Add(tagVM);
            eventBus.NotifyDomain();
            return tagVM;
        }
        
        finally { isSavingTag = false; }
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
    private bool isSavingTag;
    private ICardTagsVMHost host = null!;
    private readonly List<TagVM> allTags = [];
    private readonly ITagRepo tagRepo = tr;
    private readonly IVMEventBus eventBus = bus;
    private readonly long userId = userId;
    #endregion
}