using System.Windows;
using FlashMemo.View;
using FlashMemo.ViewModel.Windows;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace FlashMemo.Services;

public class MainWindowBootstrapper(IServiceProvider sp, ILastSessionService lss,
    ILogger<MainWindowBootstrapper> logger, IDialogService dialogs)
{
    private readonly IServiceProvider sp = sp;
    private readonly ILastSessionService lastSession = lss;

    private void SetupMainWindow(MainVM mainVM)
    {
        //* MainWindow needs to be created from DI, since its singleton.
        //* DI has reference to that window -> prevents GC from eating it.

        var win = sp.GetRequiredService<MainWindow>();
        
        win.ChangeDataCtx(mainVM);

        sp.GetRequiredService<WindowService>()
            .WireEvents(mainVM, win);

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

    public bool EnsureReadyForSwitch()
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

        return true;
    }

    public void ActivateMainVM(MainVM vm)
    {
        var mainWin = App.Current.Windows
            .OfType<MainWindow>().SingleOrDefault();

        if (mainWin is not null)
            mainWin.ChangeDataCtx(vm);

        else SetupMainWindow(vm);
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