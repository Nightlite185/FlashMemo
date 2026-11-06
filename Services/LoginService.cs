using System.Windows;
using FlashMemo.View;
using FlashMemo.ViewModel.Factories;

namespace FlashMemo.Services;

public class LoginService(MainVMF mainVMF, MainWindowBootstrapper bootstrapper, 
                            ICardService cardService, ILastSessionService lastSession,
                            IDialogService dialogs): ILoginService
{
    public async Task<bool> ChangeUser(long userId)
    {
        if (App.Current.Windows.OfType<Window>()
            .Any(window => window is not MainWindow and not UserSelectWindow))
        {
            dialogs.Show(
                title: "Close other windows",
                message: "Please close all other windows before switching users. They may contain unsaved changes.",
                buttons: DialogButtons.OK,
                icon: DialogIcons.Warning);
            return false;
        }

        await lastSession.SaveStateAsync();
        await lastSession.LoadUserCacheAsync(userId);
        await lastSession.SetLastUserAsync(userId);
        await cardService.UnburyIfNextDay();

        var newMainVM = await mainVMF.CreateAsync(userId);

        var mainWin = App.Current.Windows
            .OfType<MainWindow>().SingleOrDefault();

        if (mainWin is not null)
            bootstrapper.SetupMainVM(mainWin, newMainVM);

        else bootstrapper.SetupMainWindow(newMainVM);

        return true;
    }
}