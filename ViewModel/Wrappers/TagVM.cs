using System.Windows.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using FlashMemo.Model.Persistence;
using FlashMemo.Helpers;

namespace FlashMemo.ViewModel.Wrappers;

public partial class TagVM(Tag tag): ObservableObject, IViewModel
{
    //* manage tags VM has card and tied to it tags. When confirming changes,
    //* tag repo gets called and gets that given card, syncs it by calling SyncTags on CardEntity
    //* so the EF tracking knows the differences. Then it just saves and we're good.

    //* This TagVM doesnt need to know which card is it tied to, better if it stays dumb.
    
    private readonly Tag tag = tag;

    [ObservableProperty]
    public partial Color Color { get; set; } = tag.IntColor.ToColor();
    
    [ObservableProperty]
    public partial string Name { get; set; } = tag.Name;

    [ObservableProperty]
    public partial bool IsSelected { get; set; } = false;

    public long Id => tag.Id;
    public long UserId => tag.UserId;

    public Tag ToEntity()
    {
        tag.Name = this.Name;
        tag.IntColor = this.Color.ToInt();

        return tag;
    }
}
