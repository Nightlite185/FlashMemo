using CommunityToolkit.Mvvm.ComponentModel;
using FlashMemo.Repositories;

namespace FlashMemo.ViewModel.Windows;

public partial class UserOptionsVM(long userId, IUserOptionsRepo optRepo): ObservableObject, IViewModel
{
    private readonly long userId = userId;
    private readonly IUserOptionsRepo repo = optRepo;

    internal async Task InitAsync()
    {
        // TODO: init options from repo here
    }
}