using FlashMemo.ViewModel.Windows;
using FlashMemo.Repositories;
using FlashMemo.Services;
using FlashMemo.Helpers;

namespace FlashMemo.ViewModel.Factories;

public class UserSelectVMF(IUserRepo ur, IWindowService ws, IUserVMBuilder uvmb)
{
    private readonly IUserRepo userRepo = ur;
    private readonly IWindowService windowService = ws;
    private readonly IUserVMBuilder userVMBuilder = uvmb;

    public async Task<UserSelectVM> CreateAsync()
    {
        var vm = new UserSelectVM
            (userRepo, windowService, userVMBuilder);
    
        vm.Users.AddRange(
            await userVMBuilder.BuildAllCounted());

        return vm;
    }
}