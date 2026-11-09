using System.Windows;
using FlashMemo.View;
using FlashMemo.ViewModel.Factories;

namespace FlashMemo.Services;

public class LoginService(MainVMF mainVMF, MainWindowBootstrapper boot, 
                            ICardService cardService, ILastSessionService lastSession,
                            IDialogService dialogs): ILoginService
{
    public async Task<bool> ChangeUser(long userId)
    {
        if (!boot.EnsureReadyForSwitch())
            return false;

        await lastSession.SaveStateAsync();
        await lastSession.LoadUserCacheAsync(userId);
        await lastSession.SetLastUserAsync(userId);
        await cardService.UnburyIfNextDay();

        var newMainVM = await mainVMF.CreateAsync(userId);

        boot.ActivateMainVM(newMainVM);

        return true;
    }
}