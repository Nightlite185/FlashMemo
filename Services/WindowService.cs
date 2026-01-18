using System.Collections.ObjectModel;
using System.Windows;
using FlashMemo.View;
using FlashMemo.ViewModel;
using Microsoft.Extensions.DependencyInjection;

namespace FlashMemo.Services
{
    public class WindowService(IServiceProvider sp)
    {
        private static readonly ReadOnlyDictionary<Type, Type> VMToWindowMap = new Dictionary<Type, Type> ()
        {
            [typeof(BrowseVM)] = typeof(BrowseWindow),
            [typeof(EditCardVM)] = typeof(EditWindow),
            [typeof(OptionsVM)] = typeof(OptionsWindow),
        }.AsReadOnly();
        private readonly IServiceProvider sp = sp;

        public void ShowDialog<TViewModel>() where TViewModel: IViewModel
        {
            Type windowType = VMToWindowMap[typeof(TViewModel)];
            
            var win = (Window)sp.GetRequiredService(windowType);
            var vm = sp.GetRequiredService<TViewModel>();

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

            if (vm is IOnLoadedHandler loadHandlerVM)
                win.Loaded += (_, _) => loadHandlerVM.LoadEventHandler();
        }
    }
}