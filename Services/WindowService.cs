using System.Windows;
using FlashMemo.View;
using FlashMemo.ViewModel;
using FlashMemo.ViewModel.Bases;
using FlashMemo.ViewModel.Factories;
using Microsoft.Extensions.DependencyInjection;

namespace FlashMemo.Services;

public class WindowService
(IServiceProvider sp, BrowseVMF bVMF, EditCardVMF ecVMF, 
CreateCardVMF ccVMF, UserSelectVMF usVMF, UserOptionsVMF uoVMF, DeckOptionsMenuVMF domVMF)
{
    #region private fields
    private readonly IServiceProvider sp = sp;
    private readonly BrowseVMF browseVMF = bVMF;
    private readonly EditCardVMF editCardVMF = ecVMF;
    private readonly CreateCardVMF createCardVMF = ccVMF;
    private readonly UserSelectVMF userSelectVMF = usVMF;
    private readonly UserOptionsVMF userOptionsVMF = uoVMF;
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
                ShowCreateCard(e);
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
        var vm = await browseVMF.CreateAsync(e.UserId);
        var win = sp.GetRequiredService<BrowseWindow>();

        WireHelper(vm, win);
    }
    private async Task ShowEditCard(EditCardNavRequest e)
    {
        var vm = await editCardVMF.CreateAsync(e.CardId, e.UserId);
        var win = sp.GetRequiredService<EditCardWindow>();

        if (e.Sender is IClosedHandler ch)
            win.Closed += (_, _) => ch.OnDialogClosed();

        WireHelper(vm, win);
    }
    private void ShowCreateCard(CreateCardNavRequest e)
    {
        var vm = createCardVMF.Create(e.TargetDeck);
        var win = sp.GetRequiredService<CreateCardWindow>();

        if (e.Sender is IClosedHandler ch)
            win.Closed += (_, _) => ch.OnDialogClosed();

        WireHelper(vm, win);
    }

    ///<param name="currentUserId">
        /// This CANNOT be null if MainWindow exists,
        /// and there is a current user logged in. 
        /// Only allowed to be null at app initialization
    /// </param>
    internal async Task ShowUserSelect(long? currentUserId)
    {
        var vm = await userSelectVMF.CreateAsync(currentUserId);
        var win = sp.GetRequiredService<UserSelectWindow>();

        WireHelper(vm, win);
    }
    private async Task ShowUserOptions(UserOptionsNavRequest e)
    {
        var vm = await userOptionsVMF.CreateAsync(e.UserId);
        var win = sp.GetRequiredService<UserOptionsWindow>();

        WireHelper(vm, win);
    }

    private async Task ShowDeckOptions(DeckOptionsNavRequest e)
    {
        var vm = await deckOptionsMenuVMF.CreateAsync(e.DeckId);
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
        if (vm is INavRequestSender nrs)
            nrs.NavRequested += NavRequestHandler;

        if (vm is ICloseRequest cr)
            cr.OnCloseRequest += win.Close;

        if (vm is IClosedHandler ch && win is not MainWindow)
            win.Closed += (_, _) => ch.OnDialogClosed();
    }
    #endregion
}