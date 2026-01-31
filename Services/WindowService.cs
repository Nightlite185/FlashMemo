using System.Collections.ObjectModel;
using System.Windows;
using FlashMemo.View;
using FlashMemo.ViewModel;
using FlashMemo.ViewModel.VMFactories;
using FlashMemo.ViewModel.WindowVMs;
using Microsoft.Extensions.DependencyInjection;

namespace FlashMemo.Services;

public class WindowService(IServiceProvider sp, BrowseVMF browseVMF): IWindowService
{
    private readonly BrowseVMF browseVMF = browseVMF;
    private static readonly ReadOnlyDictionary<Type, Type> VMToWindowMap = new Dictionary<Type, Type> ()
    {
        [typeof(BrowseVM)] = typeof(BrowseWindow),
        [typeof(MainVM)] = typeof(MainWindow),
        [typeof(EditCardVM)] = typeof(EditWindow),
        [typeof(UserOptionsVM)] = typeof(OptionsWindow)
        //[typeof(DeckOptionsVM)] = typeof(DeckOptionsWindow),
        //[typeof(ChooseUserVM)] = typeof(ChooseUserWindow),
    }.AsReadOnly();
    private readonly IServiceProvider sp = sp;

    public void ShowDialog<TViewModel>() where TViewModel: IViewModel
    {
        Type windowType = VMToWindowMap[typeof(TViewModel)];
        
        var win = (Window)sp.GetRequiredService(windowType);
        var vm = sp.GetRequiredService<TViewModel>();
        
        // TODO: get VMs from factories instead of sp for proper initialization 

        SetDataCtx(vm, win);
        WireEvents(vm, win);

        win.ShowDialog();
    }

    public async Task ShowBrowseWindow(long userId)
    {
        var vm = await browseVMF.CreateAsync(userId);
        var win = sp.GetRequiredService<BrowseWindow>();

        SetDataCtx(vm, win);
        WireEvents(vm, win);

        win.ShowDialog();
    }

    private static void SetDataCtx<TViewModel>(TViewModel vm, Window win) 
    where TViewModel: IViewModel
    {
        if (win is IViewFor<TViewModel> valid)
            valid.SetVM(vm);

        else throw new InvalidOperationException(
            $"{win.GetType().Name} does not implement IViewFor<{typeof(TViewModel).Name}>");
            
    }
    private static void WireEvents(IViewModel vm, Window win)
    {
        if (vm is ICloseRequest closable)
            closable.OnCloseRequest += win.Close;

        if (vm is ILoadedHandlerAsync loadHandlerVM)
            win.Loaded += (_, _) => loadHandlerVM.LoadEventHandler();
    }
}