using FlashMemo.ViewModel.Windows;
using FlashMemo.Repositories;
using FlashMemo.Services;
using FlashMemo.Helpers;
using FlashMemo.View;
using Microsoft.Extensions.Logging;

namespace FlashMemo.ViewModel.Factories;

public class UserSelectVMF(IUserRepo ur, IUserVMBuilder uvmb, ILoginService ls,
                            ILastSessionService lss, IUserOptionsService userOptionsService,
                            ILogger<UserSelectVM> logger, IDialogService dialogs, SafetyNet net)
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
    public async Task<UserSelectVM?> TryCreateAsync(long? currentUserId)
    {                       
        var res = await net.TryUnsafe(
            action: async () =>
            {
                //* if currentUserId is null -> there is no user logged in, and MainWindow doesn't exist
                //* this can only happen when app is still in initialization process.
        
                var windows = App.Current.Windows;

                if (currentUserId is null &&
                    windows.Any(w => w is MainWindow))
                {
                    throw new InvalidOperationException(
                    "MainWindow is instantiated, but user id provided is null.");
                }
        
                var vm = new UserSelectVM(
                    userRepo, userVMBuilder,
                    loginService, lastSession,
                    userOptionsService, logger,
                    dialogs, net, currentUserId);
    
                vm.Users.AddRange(
                    await userVMBuilder.BuildAllCounted());

                return vm;
            },
            actionTried: "open the user selection window",
            retry: true);

        return res.Success ? res.Value : null;
    }
}