using System.Windows;
using FlashMemo.View;
using FlashMemo.ViewModel.Factories;
using Microsoft.Extensions.DependencyInjection;

namespace FlashMemo.Services;

public class LoginService(MainVMF mVMF, IServiceProvider sp): ILoginService
{
    private readonly IServiceProvider sp = sp;
    private readonly MainVMF mainVMF = mVMF;

    public void ChangeUser(long userId)
    {
        bool replacedMainVM = false;
        var newMainVM = mainVMF.Create(userId);

        //* closing all windows despite Main(singleton),
        //* to avoid stale state and restart based on new user

        foreach (var view in App.Current.Windows)
        {
            if (view is Window win and not MainWindow and not UserSelectWindow)
                win.Close();

            else if (view is MainWindow main)
            {
                //TODO: unify main window creation into one place, so 
                // you can sub LSS to mainWindow.Closing ONCE and not everywhere u make it.
                
                main.ChangeDataCtx(newMainVM);
                replacedMainVM = true;
            }
        }

        if (replacedMainVM) return;

        //* MainWindow needs to be created from DI, since its singleton.
        //* DI has reference to that window -> prevents GC from eating it.
        var window = sp.GetRequiredService<MainWindow>();
        
        window.ChangeDataCtx(newMainVM);
        window.Show();
    }
}