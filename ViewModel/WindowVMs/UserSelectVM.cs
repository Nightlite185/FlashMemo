using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FlashMemo.Helpers;
using FlashMemo.Repositories;
using FlashMemo.Services;
using FlashMemo.ViewModel.WrapperVMs;

namespace FlashMemo.ViewModel.WindowVMs;

public partial class UserSelectVM: ObservableObject, IViewModel, ILoadedHandlerAsync
{
    public UserSelectVM(IUserRepo userRepo, IWindowService ws, IUserVMBuilder uvmb)
    {
        this.userRepo = userRepo;
        windowService = ws;
        userVMBuilder = uvmb;
    }

    #region public properties
    public ObservableCollection<UserVM> Users { get; private set; } = [];
    
    [ObservableProperty]
    public partial UserVM? SelectedUser { get; set; }
    #endregion

    #region private things
    private readonly IUserRepo userRepo;
    private readonly IUserVMBuilder userVMBuilder;
    private readonly IWindowService windowService;
    #endregion
    
    #region methods
    public async Task LoadEventHandler() //? Idk if this is a good pattern. It makes it non-testable without the UI.
    {                                    //TODO: maybe refactor this in the future into a small factory.
        Users.AddRange(
            await userVMBuilder.BuildAllAsync());
    }
    #endregion
    
    #region ICommands
    [RelayCommand]
    public void LoadUser()
    {
        throw new NotImplementedException();
    }

    [RelayCommand]
    public async Task RemoveUser()
    {
        throw new NotImplementedException();
    }

    [RelayCommand]
    public async Task CreateUser()
    {
        throw new NotImplementedException();
    }

    [RelayCommand]
    public async Task ChangeUserName()
    {
        throw new NotImplementedException();
    }
    #endregion
}