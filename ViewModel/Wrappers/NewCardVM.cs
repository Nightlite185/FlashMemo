using CommunityToolkit.Mvvm.ComponentModel;

namespace FlashMemo.ViewModel.Wrappers;

public partial class NewCardVM: ObservableObject, ITagsSource
{
    [ObservableProperty]
    public partial NoteVM Note { get; set; } = new StandardNoteVM();
    public List<TagVM> Tags { get; init; } = [];
}