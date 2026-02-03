using CommunityToolkit.Mvvm.ComponentModel;
using FlashMemo.Model.Domain;

namespace FlashMemo.ViewModel.Wrappers;

public partial class CardStateVM: ObservableObject, IViewModel
{
    [ObservableProperty]
    public partial CardState State { get; set; }

    [ObservableProperty]
    public partial bool IsSelected { get; set; }
}