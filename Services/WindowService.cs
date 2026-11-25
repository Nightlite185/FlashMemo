using System.Windows;
using FlashMemo.View;
using FlashMemo.ViewModel;
using FlashMemo.ViewModel.Bases;
using FlashMemo.ViewModel.Factories;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace FlashMemo.Services;

public class WindowService(IServiceProvider sp, BrowseVMF bVMF, CardEditorVMF ecVMF, 
                            CreateCardVMF ccVMF, UserSelectVMF usVMF, UserOptionsMenuVMF uoVMF,
                            DeckOptionsMenuVMF domVMF, SafetyNet net, ILogger<WindowService> logger)
{
    #region private fields
    private readonly IServiceProvider sp = sp;
    private readonly BrowseVMF browseVMF = bVMF;
    private readonly CardEditorVMF editCardVMF = ecVMF;
    private readonly CreateCardVMF createCardVMF = ccVMF;
    private readonly UserSelectVMF userSelectVMF = usVMF;
    private readonly UserOptionsMenuVMF userOptionsVMF = uoVMF;
    private readonly DeckOptionsMenuVMF deckOptionsMenuVMF = domVMF;
    #endregion

    private async Task NavRequestHandler(NavigationRequest req)
    {
        switch (req)
        {
            case BrowseNavRequest e:
                await ShowBrowse(e);
                break;

            case CreateCardNavRequest e:
                await ShowCreateCard(e);
                await Task.CompletedTask;
                break;

            case EditCardNavRequest e:
                await ShowEditCard(e);
                break;

            case UserSelectNavRequest e:
                await ShowUserSelect(e.CurrentUserId);
                break;

            case UserOptionsNavRequest e:
                await ShowUserOptions(e);
                break;

            case DeckOptionsNavRequest e:
                await ShowDeckOptions(e);
                break;
        }
    }

    #region window showing dispatchers
    private async Task ShowBrowse(BrowseNavRequest e)
    {
        var vm = await browseVMF.TryCreateAsync(e.UserId);
        if (vm is null) return;

        TryWireAndShow(vm, sp.GetRequiredService<BrowseWindow>);
    }
    private async Task ShowEditCard(EditCardNavRequest e)
    {
        var vm = await editCardVMF.TryCreateAsync(e.CardId, e.UserId);
        if (vm is null) return;

        TryWireAndShow(vm, sp.GetRequiredService<EditCardWindow>);
    }
    private async Task ShowCreateCard(CreateCardNavRequest e)
    {
        var vm = await createCardVMF.TryCreateAsync(e.TargetDeck);
        if (vm is null) return;

        TryWireAndShow(vm, sp.GetRequiredService<CreateCardWindow>);
    }

    ///<param name="currentUserId">
        /// This CANNOT be null if MainWindow exists,
        /// and there is a current user logged in. 
        /// Only allowed to be null at app initialization
    /// </param>
    internal async Task ShowUserSelect(long? currentUserId)
    {
        var vm = await userSelectVMF.TryCreateAsync(currentUserId);
        if (vm is null) return;

        TryWireAndShow(vm, sp.GetRequiredService<UserSelectWindow>);
    }
    private async Task ShowUserOptions(UserOptionsNavRequest e)
    {
        var vm = await userOptionsVMF.TryCreateAsync(e.UserId);
        if (vm is null) return;

        TryWireAndShow(vm, sp.GetRequiredService<UserOptionsWindow>);
    }

    private async Task ShowDeckOptions(DeckOptionsNavRequest e)
    {
        var vm = await deckOptionsMenuVMF.TryCreateAsync(e.DeckId);
        if (vm is null) return;

        TryWireAndShow(vm, sp.GetRequiredService<DeckOptionsWindow>);
    }
    #endregion
    
    #region private helpers
    private bool TryWireAndShow<TVM, TWindow>(TVM vm, Func<TWindow> getWindow)
    where TVM: class, IViewModel
    where TWindow: Window
    {
        TWindow? win = null;

        var attempt = net.TryUnsafeSync(
            action: () =>
            {
                win = getWindow();
                SetDataCtx(vm, win);
                WireEvents(vm, win);
                return win.ShowDialog();
            },
            actionTried: $"initialize and show {typeof(TWindow)}",
            retry: false);

        if (!attempt.Success)
        {
            try { win?.Close(); }

            catch(Exception e) {
                logger.LogError(e, "Failed to close the window as cleanup after failed ShowDialog attempt.");
            }

            Cleanup(vm, win);
            return false;
        }

        return true;
    }
    private static void SetDataCtx<TVM>(TVM vm, Window win) where TVM: class, IViewModel
    {
        if (win is IViewFor<TVM> valid)
        {
            valid.VM = vm;
            win.DataContext = vm;
        }

        else throw new InvalidOperationException(
            $"{win.GetType().Name} does not implement IViewFor<{typeof(TVM).Name}>");
    }
    
    private void Cleanup(object? vm, Window? win)
    {
        if (vm is INavRequestSender nav)
            nav.NavRequested -= NavRequestHandler;
            
        if (vm is ICloseRequest close && win is not null)
            close.OnCloseRequest -= win.Close;
            
        if (vm is IClosedHandler cleanup)
            cleanup.OnClosed();
    }
    internal void WireEvents(IViewModel vm, Window win)
    {
        win.DataContext = vm;
        attachVM(vm);

        // Window handlers are attached once and always use the current VM.
        win.DataContextChanged += (_, e) =>
        {
            Cleanup(e.OldValue, win);
            attachVM(e.NewValue);
        };

        win.Closed += (_, _) => Cleanup(win.DataContext, win);

        win.Activated += async (_, _) =>
        {
            if (App.IsFatalShutdownStarted)
                return;

            if (win.DataContext is IFocusState focusable)
                await focusable.OnFocusGained();
        };
        
        win.Deactivated += (_, _) =>
        {
            if (App.IsFatalShutdownStarted)
                return;

            if (win.DataContext is IFocusState focusable)
                focusable.OnFocusLost();
        };

        bool checkingClose = false;
        bool allowClose = false;

        win.Closing += async (_, e) =>
        {
            if (App.IsFatalShutdownStarted)
                return;

            if (allowClose)
            {
                allowClose = false;
                return;
            }

            if (win.DataContext is not IClosingAware current)
                return;

            // WPF does not await Closing handlers: cancel before the first await.
            e.Cancel = true;
            if (checkingClose)
                return;

            checkingClose = true;
            try
            {
                if (await current.CanCloseAsync())
                {
                    // Queue the approved close so a synchronous decision cannot reenter Closing.
                    await win.Dispatcher.InvokeAsync(() =>
                    {
                        allowClose = true;
                        win.Close();
                    });
                }
            }
            finally { checkingClose = false; }
        };

        void attachVM(object? vm)
        {
            if (vm is INavRequestSender navigation)
                navigation.NavRequested += NavRequestHandler;
            if (vm is ICloseRequest close)
                close.OnCloseRequest += win.Close;
        }
    }
    #endregion
}