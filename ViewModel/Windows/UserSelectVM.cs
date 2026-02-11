using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FlashMemo.Model.Persistence;
using FlashMemo.Repositories;
using FlashMemo.Services;
using FlashMemo.ViewModel.Wrappers;

namespace FlashMemo.ViewModel.Windows;

public partial class UserSelectVM: ObservableObject, IViewModel, ICloseRequest
{
    public UserSelectVM(IUserRepo ur, IUserVMBuilder uvmb, ILoginService ls, ILastSessionService lss, long? currentUserId = null)
    {
        userRepo = ur;
        loginService = ls;
        lastSession = lss;
        userVMBuilder = uvmb;
        this.currentUserId = currentUserId;
    }

    #region public properties
    public ObservableCollection<UserVM> Users { get; init; } = [];
    
    [ObservableProperty]
    public partial UserVM? SelectedUser { get; set; }
    public event Action? OnCloseRequest;
    #endregion

    #region private things
    private readonly ILastSessionService lastSession;
    private readonly IUserRepo userRepo;
    private readonly IUserVMBuilder userVMBuilder;
    private readonly ILoginService loginService;
    private readonly long? currentUserId;
    #endregion
    
    #region ICommands
    [RelayCommand]
    private async Task RemoveUser(UserVM toRemove)
    {   
        //TODO: this button only visible when its not current user thats selected
        //TODO: show "you sure?? it will cascade cards and decks!!" pop up here;

        if (currentUserId == toRemove.Id)
            throw new InvalidOperationException(
            "Cannot remove user that you're currently logged in with.");

        await userRepo.Remove(toRemove.Id);
        Users.Remove(toRemove);
    }

    [RelayCommand]
    private async Task CreateUser(string name)
    {
        var user = UserEntity.Create(name);
        var vm = userVMBuilder.BuildUncounted(user);

        await userRepo.CreateNew(user);
        
        Users.Add(vm);
    }

    [RelayCommand]
    private async Task Login(UserVM toLogin) // bind this as CommandParam in xaml.
    {
        //TODO: button hidden when the selected user is current one.

        if (currentUserId == toLogin.Id)
            throw new InvalidOperationException(
            "Cannot log in with user that you're already logged in with.");

        loginService.ChangeUser(toLogin.Id);
        lastSession.Current.LastLoadedUserId = toLogin.Id;

        OnCloseRequest?.Invoke();
    }

    #endregion
}