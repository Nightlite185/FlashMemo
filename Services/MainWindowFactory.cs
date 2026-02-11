using FlashMemo.View;
using FlashMemo.ViewModel.Windows;
using Microsoft.Extensions.DependencyInjection;

namespace FlashMemo.Services;

public class MainWindowFactory(IServiceProvider sp, ILastSessionService lss)
{
    private readonly IServiceProvider sp = sp;
    private readonly ILastSessionService lastSession = lss;

    public void Resolve(MainVM mainVM)
    {
        //* MainWindow needs to be created from DI, since its singleton.
        //* DI has reference to that window -> prevents GC from eating it.

        var win = sp.GetRequiredService<MainWindow>();
        
        win.ChangeDataCtx(mainVM);
        
        sp.GetRequiredService<WindowService>()
            .WireEvents(mainVM, win);
        
        win.Closing += async (_, _) => 
            await lastSession.SaveStateAsync();
            
        win.Show();
    }
}