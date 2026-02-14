using System.Windows;
using FlashMemo.Model.Persistence;
using FlashMemo.View;
using FlashMemo.ViewModel;
using FlashMemo.ViewModel.Bases;
using FlashMemo.ViewModel.Factories;
using Microsoft.Extensions.DependencyInjection;

namespace FlashMemo.Services;

public class WindowService
(IServiceProvider sp, BrowseVMF bVMF, EditCardVMF ecVMF, 
CreateCardVMF ccVMF, UserSelectVMF usVMF, UserOptionsVMF uoVMF)
{
    #region private fields
    private readonly IServiceProvider sp = sp;
    private readonly BrowseVMF browseVMF = bVMF;
    private readonly EditCardVMF editCardVMF = ecVMF;
    private readonly CreateCardVMF createCardVMF = ccVMF;
    private readonly UserSelectVMF userSelectVMF = usVMF;
    private readonly UserOptionsVMF userOptionsVMF = uoVMF;
    #endregion

    private async Task NavRequestHandler(NavigationRequest req)
    {
        switch (req)
        {
            case BrowseNavRequest b:
                await ShowBrowse(b.UserId);
                break;

            case CreateCardNavRequest cc:
                ShowCreateCard(cc.TargetDeck);
                await Task.CompletedTask;
                break;

            case EditCardNavRequest ec:
                await ShowEditCard(ec.CardId, ec.UserId);
                break;

            case UserSelectNavRequest us:
                await ShowUserSelect(us.CurrentUserId);
                break;

            case UserOptionsNavRequest uo:
                await ShowUserOptions(uo.UserId);
                break;
        }
    }

    #region window showing dispatchers
    private async Task ShowBrowse(long userId)
    {
        var vm = await browseVMF.CreateAsync(userId);
        var win = sp.GetRequiredService<BrowseWindow>();

        WireHelper(vm, win);
    }
    private async Task ShowEditCard(long cardId, long userId)
    {
        var vm = await editCardVMF.CreateAsync(cardId, userId);
        var win = sp.GetRequiredService<EditCardWindow>();

        WireHelper(vm, win);
    }
    private void ShowCreateCard(IDeckMeta targetDeck)
    {
        var vm = createCardVMF.Create(targetDeck);
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
        var vm = await userSelectVMF.CreateAsync(currentUserId);
        var win = sp.GetRequiredService<UserSelectWindow>();

        WireHelper(vm, win);
    }
    private async Task ShowUserOptions(long userId)
    {
        var vm = await userOptionsVMF.CreateAsync(userId);
        var win = sp.GetRequiredService<UserOptionsWindow>();

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
        if (vm is INavRequestSender reqSender)
            reqSender.NavRequested += NavRequestHandler;

        if (vm is ICloseRequest closable)
            closable.OnCloseRequest += win.Close;
    }
    #endregion
}