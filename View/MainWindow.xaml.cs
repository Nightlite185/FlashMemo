using FlashMemo.ViewModel.Windows;
using System.Windows;
using System.Windows.Controls;

namespace FlashMemo.View
{
    public interface IViewFor<TViewModel>
    {
        TViewModel VM { get; set; }
    }
    public partial class MainWindow: Window, IViewFor<MainVM>
    {
        public MainVM VM { get; set; } = null!;
        internal MainWindow()
        {
            InitializeComponent();
        }

        private void DeckOptions_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button bt && bt.ContextMenu != null)
            {
                bt.ContextMenu.PlacementTarget = bt;
                bt.ContextMenu.IsOpen = true;
            }
        }
    }
}