using System.Windows;
using Microsoft.Extensions.DependencyInjection;

namespace FlashMemo.Services
{
    public class WindowService(IServiceProvider sp)
    {
        private readonly IServiceProvider sp = sp;

        public void ShowWindow<TWindow>() where TWindow : Window
        {
            var win = sp.GetRequiredService<TWindow>();
            win.ShowDialog();
        }
    }
}