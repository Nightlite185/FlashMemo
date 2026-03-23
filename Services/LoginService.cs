using System.Windows;
using FlashMemo.View;
using FlashMemo.ViewModel.Factories;

namespace FlashMemo.Services;

public class LoginService(MainVMF mainVMF, MainWindowBootstrapper bootstrapper, 
                            ICardService cardService): ILoginService
{
    public async Task ChangeUser(long userId)
    {
        await cardService.UnburyIfNextDay();

        bool replacedMainVM = false;
        var newMainVM = mainVMF.Create(userId);

        //* closing all windows despite Main(singleton),
        //* to avoid stale state and restart based on new user

        foreach (var view in App.Current.Windows)
        {
            if (view is Window win
            and not MainWindow
            and not UserSelectWindow)
                win.Close();

            else if (view is MainWindow mainWin)
            {
                bootstrapper.SetupMainVM(mainWin, newMainVM);
                replacedMainVM = true;
            }
        }

        if (!replacedMainVM)
            bootstrapper.SetupMainWindow(newMainVM);
    }
}