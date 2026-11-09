using System.Windows;
using FlashMemo.View;
using FlashMemo.ViewModel;
using FlashMemo.ViewModel.Bases;
using FlashMemo.ViewModel.Factories;
using FlashMemo.ViewModel.Windows;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace FlashMemo.Services;

public class WindowService
(IServiceProvider sp, BrowseVMF bVMF, CardEditorVMF ecVMF, 
CreateCardVMF ccVMF, UserSelectVMF usVMF, UserOptionsMenuVMF uoVMF, DeckOptionsMenuVMF domVMF,
    ILogger<WindowService> logger)
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

        var win = sp.GetRequiredService<BrowseWindow>();

        WireHelper(vm, win);
    }
    private async Task ShowEditCard(EditCardNavRequest e)
    {
        var vm = await editCardVMF.TryCreateAsync(e.CardId, e.UserId);
        if (vm is null) return;

        var win = sp.GetRequiredService<EditCardWindow>();

        WireHelper(vm, win);
    }
    private async Task ShowCreateCard(CreateCardNavRequest e)
    {
        var vm = await createCardVMF.TryCreateAsync(e.TargetDeck);
        if (vm is null) return;

        var win = sp.GetRequiredService<CreateCardWindow>();

        WireHelper(vm, win);
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

        var win = sp.GetRequiredService<UserSelectWindow>();

        WireHelper(vm, win);
    }
    private async Task ShowUserOptions(UserOptionsNavRequest e)
    {
        var vm = await userOptionsVMF.TryCreateAsync(e.UserId);
        if (vm is null) return;

        var win = sp.GetRequiredService<UserOptionsWindow>();

        WireHelper(vm, win);
    }

    private async Task ShowDeckOptions(DeckOptionsNavRequest e)
    {
        var vm = await deckOptionsMenuVMF.TryCreateAsync(e.DeckId);
        if (vm is null) return;

        var win = sp.GetRequiredService<DeckOptionsWindow>();

        WireHelper(vm, win);
    }
    #endregion
    
    #region private helpers
    private void WireHelper<TVM>(TVM vm, Window win) where TVM: class, IViewModel
    {
        SetDataCtx(vm, win);
        WireEvents(vm, win);

        win.ShowDialog();
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
    internal void WireEvents(IViewModel vm, Window win)
    {
        win.DataContext = vm;
        AttachVM(vm);

        // Window handlers are attached once and always use the current VM.
        win.DataContextChanged += (_, e) =>
        {
            DetachVM(e.OldValue);
            (e.OldValue as IClosedHandler)?.OnClosed();
            AttachVM(e.NewValue);
        };

        win.Closed += (_, _) =>
        {
            DetachVM(win.DataContext);
            (win.DataContext as IClosedHandler)?.OnClosed();
        };

        win.Activated += async (_, _) =>
        {
            if (win.DataContext is IFocusState current)
                await current.OnFocusGained();
        };
        win.Deactivated += (_, _) => (win.DataContext as IFocusState)?.OnFocusLost();

        bool checkingClose = false;
        bool allowClose = false;
        win.Closing += async (_, e) =>
        {
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

        void AttachVM(object? model)
        {
            if (model is INavRequestSender navigation)
                navigation.NavRequested += NavRequestHandler;
            if (model is ICloseRequest close)
                close.OnCloseRequest += win.Close;
        }

        void DetachVM(object? model)
        {
            // VM-owned events must release their subscriptions when its window closes or VM changes.
            if (model is INavRequestSender navigation)
                navigation.NavRequested -= NavRequestHandler;
            if (model is ICloseRequest close)
                close.OnCloseRequest -= win.Close;
        }
    }
    #endregion
}