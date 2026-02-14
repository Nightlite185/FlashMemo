using FlashMemo.View;
using FlashMemo.ViewModel.Windows;
using Microsoft.Extensions.DependencyInjection;

namespace FlashMemo.Services;

public class MainWindowBootstrapper(IServiceProvider sp, ILastSessionService lss)
{
    private readonly IServiceProvider sp = sp;
    private readonly ILastSessionService lastSession = lss;

    public void SetupMainWindow(MainVM mainVM)
    {
        //* MainWindow needs to be created from DI, since its singleton.
        //* DI has reference to that window -> prevents GC from eating it.

        var win = sp.GetRequiredService<MainWindow>();
        
        SetupMainVM(win, mainVM);
        
        win.Closing += async (_, _) => 
            await lastSession.SaveStateAsync();
            
        win.Show();
    }

    public void SetupMainVM(MainWindow win, MainVM vm)
    {
        win.ChangeDataCtx(vm);

        sp.GetRequiredService<WindowService>()
            .WireEvents(vm, win);
    }
}