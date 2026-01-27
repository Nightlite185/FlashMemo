using System.Windows.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using FlashMemo.Model.Domain;
using FlashMemo.Model.Persistence;

namespace FlashMemo.ViewModel.WrapperVMs;

public partial class TagVM(Tag tag): ObservableObject, IViewModel
{
    //* manage tags VM has card and tied to it tags. When confirming changes,
    //* tag repo gets called and gets that given card, syncs it by calling SyncTags on CardEntity
    //* so the EF tracking knows the differences. Then it just saves and we're good.

    //* This TagVM doesnt need to know which card is it tied to, better if it stays dumb.
    
    private readonly Tag tag = tag;

    [ObservableProperty]
    public partial Color Color { get; set; }
    
    [ObservableProperty]
    public partial string Name { get; set; }

    [ObservableProperty]
    public partial bool IsSelected { get; set; }

    public long Id => tag.Id;
    public long UserId => tag.UserId;

    public Tag ToEntity()
    {
        tag.Name = this.Name;
        // TODO convert color to int here

        return tag;
    }
}

public partial class CardStateVM: ObservableObject
{
    [ObservableProperty]
    public partial CardState State { get; set; }

    [ObservableProperty]
    public partial bool IsSelected { get; set; }
}
