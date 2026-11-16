using FlashMemo.ViewModel.Factories;
using FlashMemo.ViewModel.Windows;
using Microsoft.Extensions.Logging;

namespace FlashMemo.Services;

public class LoginService(MainVMF mainVMF, IMainWindowBootstrapper boot,
                            ICardService cardService, ILastSessionService lastSession,
                            IDialogService dialogs, SafetyNet net, ILogger<LoginService> logger): ILoginService
{
    public async Task<bool> ChangeUser(long userId)
    {
        if (!boot.EnsureReadyForSwitch())
            return false;

        try { await lastSession.SaveStateAsync(); }
        catch (Exception e)
        {
            logger.LogError(e, 
                "Failed to save the previous user's session before switching users");
        }

        await lastSession.LoadUserCacheAsync(userId);

        await net.TryUnsafe(
            cardService.UnburyIfNextDay,
            actionTried: "unbury cards from previous days. You can continue, but some cards may remain buried until this succeeds.",
            retry: false);

        MainVM? newMainVM = null;
        
        try
        {
            newMainVM = await mainVMF.CreateAsync(userId);
            boot.ActivateMainVM(newMainVM);
        }

        catch (Exception e)
        {
            newMainVM?.OnClosed();
            logger.LogCritical(e, "Failed to open the main view for user {UserId}", userId);
            dialogs.ShowError(
                message: "Something went wrong and we couldn't open the main window. The program will close after this pop-up.",
                title: "Fatal error");

            boot.Shutdown();
            return false;
        }

        // Remembering a user affects the next launch, not the successful login.
        try { await lastSession.SetLastUserAsync(userId); }
        catch (Exception e)
        {
            logger.LogError(e, 
                "Failed to remember the logged-in user {UserId}", userId);
        }

        return true;
    }
}