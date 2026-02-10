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
    public UserSelectVM(IUserRepo ur, IUserVMBuilder uvmb, ILoginService ls)
    {
        userRepo = ur;
        loginService = ls;
        userVMBuilder = uvmb;
    }

    #region public properties
    public ObservableCollection<UserVM> Users { get; init; } = [];
    
    [ObservableProperty]
    public partial UserVM? SelectedUser { get; set; }
    public event Action? OnCloseRequest;
    #endregion

    #region private things
    private readonly IUserRepo userRepo;
    private readonly IUserVMBuilder userVMBuilder;
    private readonly ILoginService loginService;
    #endregion
    
    #region ICommands
    [RelayCommand]
    public async Task RemoveUser(UserVM user)
    {
        // show "you sure?? it will cascade cards and decks!!" pop up here;

        await userRepo.Remove(user.Id);
        Users.Remove(user);
    }

    [RelayCommand]
    public async Task CreateUser(string name)
    {
        var user = UserEntity.Create(name);
        var vm = userVMBuilder.BuildUncounted(user);

        await userRepo.CreateNew(user);
        
        Users.Add(vm);
    }

    [RelayCommand]
    private async Task Login(UserVM user) // bind this as CommandParam in xaml.
    {
        loginService.ChangeUser(user.Id);
        // OnCloseRequest?.Invoke(); //? prolly unneeded since LoginService closes everything already.
    }

    #endregion
}