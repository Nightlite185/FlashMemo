using System.Windows;
using FlashMemo.View;
using FlashMemo.ViewModel.Factories;

namespace FlashMemo.Services;

public class LoginService(MainVMF mVMF, MainWindowFactory mwf): ILoginService
{
    private readonly MainWindowFactory winFactory = mwf;
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
                main.ChangeDataCtx(newMainVM);
                replacedMainVM = true;
            }
        }

        if (!replacedMainVM)
            winFactory.Resolve(newMainVM);
    }
}