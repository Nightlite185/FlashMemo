using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using FlashMemo.Model.Persistence;

namespace FlashMemo.ViewModel.Wrappers;

public partial class NewCardVM(Deck deck): ObservableObject, IViewModel
{
    [ObservableProperty]
    public partial Deck Deck { get; set; } = deck;

    [ObservableProperty]
    public partial string FrontContent { get; set; } = "";

    [ObservableProperty]
    public partial string BackContent { get; set; } = "";

    public ObservableCollection<TagVM> Tags { get; init; } = [];
}