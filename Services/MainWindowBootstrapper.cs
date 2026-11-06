using FlashMemo.View;
using FlashMemo.ViewModel.Windows;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace FlashMemo.Services;

public class MainWindowBootstrapper(IServiceProvider sp, ILastSessionService lss,
    ILogger<MainWindowBootstrapper> logger)
{
    private readonly IServiceProvider sp = sp;
    private readonly ILastSessionService lastSession = lss;

    public void SetupMainWindow(MainVM mainVM)
    {
        //* MainWindow needs to be created from DI, since its singleton.
        //* DI has reference to that window -> prevents GC from eating it.

        var win = sp.GetRequiredService<MainWindow>();
        
        SetupMainVM(win, mainVM);

        sp.GetRequiredService<WindowService>().WireEvents(mainVM, win);

        bool isExiting = false;
        win.Closing += async (_, e) =>
        {
            if (isExiting)
                return;

            if (win.DataContext is MainVM { IsOpeningDialog: true })
            {
                e.Cancel = true;
                return;
            }

            isExiting = true;
            
            // Let the Closing event finish before saving and requesting application shutdown.
            await Task.Yield();
            await SaveSessionOnExitAsync();
            App.Current.Shutdown();
        };
            
        win.Show();
    }

    public void SetupMainVM(MainWindow win, MainVM vm)
    {
        win.ChangeDataCtx(vm);
    }

    internal async Task SaveSessionOnExitAsync()
    {
        try { await lastSession.SaveStateAsync(); }
        catch (Exception e)
        {
            logger.LogError(e, 
                "Failed to save the last session before exiting");
        }
    }
}