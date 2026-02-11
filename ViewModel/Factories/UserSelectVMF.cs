using FlashMemo.ViewModel.Windows;
using FlashMemo.Repositories;
using FlashMemo.Services;
using FlashMemo.Helpers;
using FlashMemo.View;

namespace FlashMemo.ViewModel.Factories;

public class UserSelectVMF(IUserRepo ur, IUserVMBuilder uvmb, ILoginService ls, ILastSessionService lss)
{
    private readonly IUserRepo userRepo = ur;
    private readonly ILastSessionService lastSession = lss;
    private readonly IUserVMBuilder userVMBuilder = uvmb;
    private readonly ILoginService loginService = ls;

    ///<param name="currentUserId">
        /// This CANNOT be null if MainWindow exists,
        /// and there is a current user logged in. 
        /// Only allowed to be null at app initialization
    /// </param>
    public async Task<UserSelectVM> CreateAsync(long? currentUserId)
    {                       
        //* if currentUserId is null -> there is no user logged in, and MainWindow doesn't exist
        //* this can only happen when app is still in initialization process.
        
        var windows = App.Current.Windows;

        if (currentUserId is null && 
            windows.Any(w => w is MainWindow))
        {
            throw new InvalidOperationException(
            "MainWindow is instantiated, and you didn't provide currentUserId");
        }
        
        var vm = new UserSelectVM(
            userRepo, userVMBuilder, 
            loginService, lastSession, currentUserId);
    
        vm.Users.AddRange(
            await userVMBuilder.BuildAllCounted());

        return vm;
    }
}