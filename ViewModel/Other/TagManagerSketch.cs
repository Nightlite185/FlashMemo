using FlashMemo.ViewModel.Wrappers;

namespace FlashMemo.ViewModel.Other;

public interface ITagManagerEventSource
{
    event Action<IEnumerable<TagVM>, long> TagContextChanged;
    event Action TagContextCleared;
}

public abstract class CodexTagManagerVM : ITagManagerVM
{
    public abstract IEnumerable<TagVM> CardTags { get; }

    public abstract event Action TagsChanged;
    public abstract event Action<string> TagValidationFailed;

    public abstract void AttachToHostEvents(ITagManagerEventSource host);
    public abstract void DetachFromHostEvents(ITagManagerEventSource host);

    public abstract Task<IEnumerable<TagVM>> GetAllExistingTagsAsync();
    public abstract Task<TagVM?> AddTagAsync(string tagName);
    public abstract bool RemoveTag(long tagId);

    public abstract void ReplaceLocalTags(IEnumerable<TagVM> tags);
    public abstract void ClearState();
}
