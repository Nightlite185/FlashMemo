using CommunityToolkit.Mvvm.ComponentModel;

namespace FlashMemo.ViewModel.Windows;

public partial class UserOptionsVM(long userId): ObservableObject, IViewModel
{
    private readonly long userId = userId;
}