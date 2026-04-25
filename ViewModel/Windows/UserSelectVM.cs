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
        if (currentUserId == toRemove.Id)
        {
            DialogService.Show(
                title: "Invalid action",
                message: "Sorry, but you can't delete the user that you're currently logged in with. Please log in to another user, then delete this one.",
                buttons: DialogButtons.OK,
                icon: DialogIcons.Information
            );

            return;
        }

        var answer = DialogService.Show(
            title: "Are you sure?",
            message: "Are you sure you want to delete this user? This action can't be undone, and will delete every deck, card, etc. related with this user! Do you still wish to proceed?",

            buttons: DialogButtons.YesNo,
            icon: DialogIcons.Warning
        );

        if (answer is DialogResult.No) 
            return;

        await userRepo.Remove(toRemove.Id);
        Users.Remove(toRemove);

        if (lastSession.LastUserId == toRemove.Id)
            lastSession.LastUserId = null;
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
            await loginService.ChangeUser(toLogin.Id);
            lastSession.LastUserId = toLogin.Id;
        }

        OnCloseRequest?.Invoke();
    }
    #endregion
}