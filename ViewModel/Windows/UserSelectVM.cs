using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FlashMemo.Model.Persistence;
using FlashMemo.Repositories;
using FlashMemo.Services;
using FlashMemo.ViewModel.Wrappers;

namespace FlashMemo.ViewModel.Windows;

public partial class UserSelectVM: ObservableObject, IViewModel
{
    public UserSelectVM(IUserRepo userRepo, IWindowService ws, IUserVMBuilder uvmb)
    {
        this.userRepo = userRepo;
        windowService = ws;
        userVMBuilder = uvmb;
    }

    #region public properties
    public ObservableCollection<UserVM> Users { get; init; } = [];
    
    [ObservableProperty]
    public partial UserVM? SelectedUser { get; set; }
    #endregion

    #region private things
    private readonly IUserRepo userRepo;
    private readonly IUserVMBuilder userVMBuilder;
    private readonly IWindowService windowService;
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
    #endregion
}