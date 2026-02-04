using System.Collections.ObjectModel;
using System.Windows;
using FlashMemo.Model.Persistence;
using FlashMemo.View;
using FlashMemo.ViewModel;
using FlashMemo.ViewModel.Factories;
using FlashMemo.ViewModel.Windows;
using Microsoft.Extensions.DependencyInjection;

namespace FlashMemo.Services;

public class WindowService
(IServiceProvider sp, BrowseVMF bVMF, EditCardVMF ecVMF, 
CreateCardVMF ccVMF, UserSelectVMF usVMF)
: IWindowService
{
    private readonly BrowseVMF browseVMF = bVMF;
    private readonly EditCardVMF editCardVMF = ecVMF;
    private readonly CreateCardVMF createCardVMF = ccVMF;
    private readonly UserSelectVMF userSelectVMF = usVMF;
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

        WireHelper(vm, win);
    }

    public async Task ShowBrowseWindow(long userId)
    {
        var vm = await browseVMF.CreateAsync(userId);
        var win = sp.GetRequiredService<BrowseWindow>();

        WireHelper(vm, win);
    }

    public async Task ShowEditCardWindow(long cardId, long userId)
    {
        var vm = await editCardVMF.CreateAsync(cardId, userId);
        var win = sp.GetRequiredService<EditWindow>();

        WireHelper(vm, win);
    }

    public void ShowCreateCardWindow(Deck targetDeck)
    {
        var vm = createCardVMF.Create(targetDeck);
        var win = sp.GetRequiredService<CreateCardWindow>();

        WireHelper(vm, win);
    }

    public async Task ShowUserSelectWindow()
    {
        var vm = await userSelectVMF.CreateAsync();
        var win = sp.GetRequiredService<UserSelectWindow>();

        WireHelper(vm, win);
    }

    private static void WireHelper(IViewModel vm, Window win)
    {
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