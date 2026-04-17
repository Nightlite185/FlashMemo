using CommunityToolkit.Mvvm.ComponentModel;
using FlashMemo.Model.Persistence;

namespace FlashMemo.ViewModel.Wrappers;

public partial class TagVM(Tag tag): ObservableObject, IViewModel
{
    private readonly Tag tag = tag;

    [ObservableProperty]
    public partial string Name { get; set; } = tag.Name;

    [ObservableProperty]
    public partial bool IsSelected { get; set; } = false;

    public long Id => tag.Id;
    public long UserId => tag.UserId;

    public Tag ToEntity()
    {
        tag.Name = this.Name;
        return tag;
    }
}
