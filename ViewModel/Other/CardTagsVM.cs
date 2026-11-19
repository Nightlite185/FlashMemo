using CommunityToolkit.Mvvm.ComponentModel;
using System.Collections.ObjectModel;
using FlashMemo.Helpers;
using FlashMemo.Model.Persistence;
using FlashMemo.Repositories;
using FlashMemo.Services;
using FlashMemo.ViewModel.Wrappers;
using System.Collections.Specialized;

namespace FlashMemo.ViewModel.Other;

public partial class CardTagsVM(ITagRepo tr, IVMEventBus bus, long userId, SafetyNet net)
    : ObservableObject, ICardTagsVM, IViewModel
{
    #region public properties
    public ObservableCollection<TagVM> SelectedTags { get; } = [];
    public ObservableCollection<TagVM> AllTags { get; } = [];
    #endregion

    #region methods
    internal async Task InitAsync(ICardTagsVMHost host)
    {
        this.host = host;

        var allTagVMs = (await tagRepo
            .GetFromUser(userId))
            .ToVMs();

        AllTags.Clear();
        AllTags.AddRange(allTagVMs);
        ReloadSelection();
    }

    // Returns the selected tag (including an already selected one), or null on failure/no submission.
    public async Task<TagVM?> TryAddTagAsync(string tagName)
    {
        tagName = tagName.Trim();
        if (isSavingTag || string.IsNullOrWhiteSpace(tagName))
            return null;

        static bool sameName(string a, string b)
            => string.Equals(a, b, StringComparison.OrdinalIgnoreCase);

        if (SelectedTags.FirstOrDefault(t => sameName(t.Name, tagName)) is TagVM selected)
            return selected;

        isSavingTag = true;

        try
        {
            var tagVM = AllTags.SingleOrDefault(t => sameName(t.Name, tagName));

            if (tagVM is null)
            {
                var result = await net.TryUnsafe(
                    () => tagRepo.GetOrCreate(Tag.CreateNew(tagName, userId)),
                    actionTried: "add the tag. Your current tags and entered text have been kept; you can try again.",
                    retry: false);

                if (!result.Success)
                    return null;

                tagVM = new TagVM(result.Value);
                AllTags.Add(tagVM);
            }

            SelectedTags.Add(tagVM);
            eventBus.NotifyDomain();
            return tagVM;
        }
        
        finally { isSavingTag = false; }
    }

    public void ReloadSelection()
    {
        // The host changes its draft after Add/Revert. Reuse suggestion instances so
        // Syncfusion can exclude selected tags from its dropdown by object identity.
        TagVM[] tags = [..host.Tags.Select(tag => AllTags.FirstOrDefault(t => t.Id == tag.Id) ?? tag)];
        SelectedTags.CollectionChanged -= OnSelectionChanged;
        SelectedTags.Clear();
        SelectedTags.AddRange(tags);
        SelectedTags.CollectionChanged += OnSelectionChanged;
    }

    private void OnSelectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        host.Tags.Clear();
        host.Tags.AddRange(SelectedTags);
    }
    #endregion

    #region private things
    private bool isSavingTag;
    private ICardTagsVMHost host = null!;
    private readonly ITagRepo tagRepo = tr;
    private readonly IVMEventBus eventBus = bus;
    private readonly long userId = userId;
    #endregion
}