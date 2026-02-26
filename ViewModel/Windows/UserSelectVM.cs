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
        NewUsernameField = "";
    }

    public bool IsNameAvailable(string name)
        => !Users.Any(u => u.Name == name);

    #region public properties
    public ObservableCollection<UserVM> Users { get; init; } = [];

    [ObservableProperty]
    public partial string NewUsernameField { get; set; }
    
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
    private async Task RenameUser(UserVM user)
    {
        string newName = user.CommitRename();
        await userRepo.Rename(user.Id, newName);
    }

    [RelayCommand]
    private async Task RemoveUser(UserVM toRemove)
    {
        //TODO: this button only visible when the current logged user is not the one to remove.

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
    private async Task Login(UserVM toLogin)
    {
        if (currentUserId != toLogin.Id)
        {
            loginService.ChangeUser(toLogin.Id);
            lastSession.UpdateUser(toLogin.Id);
        }

        OnCloseRequest?.Invoke();
    }
    #endregion
}