using System.Windows;
using FlashMemo.ViewModel;
using Microsoft.Extensions.DependencyInjection;

namespace FlashMemo.Services
{
    public class WindowService(IServiceProvider sp)
    {
        private readonly IServiceProvider sp = sp;

        public void ShowWindow<TWindow>(IViewModel vm) where TWindow : Window // TO DO: T being vm, and window service-
        {                                                                    // -auto decides which win to show, based on a dict or sth.
            var win = sp.GetRequiredService<TWindow>();
            win.ShowDialog();
        }
    }
}