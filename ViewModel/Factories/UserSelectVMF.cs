using FlashMemo.ViewModel.Windows;
using FlashMemo.Repositories;
using FlashMemo.Services;
using FlashMemo.Helpers;

namespace FlashMemo.ViewModel.Factories;

public class UserSelectVMF(IUserRepo ur, IUserVMBuilder uvmb, ILoginService ls)
{
    private readonly IUserRepo userRepo = ur;
    private readonly IUserVMBuilder userVMBuilder = uvmb;
    private readonly ILoginService loginService = ls;

    public async Task<UserSelectVM> CreateAsync()
    {
        var vm = new UserSelectVM(
            userRepo, userVMBuilder, 
            loginService);
    
        vm.Users.AddRange(
            await userVMBuilder.BuildAllCounted());

        return vm;
    }
}