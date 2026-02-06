using FlashMemo.ViewModel.Windows;
using System.Windows;
using System.Windows.Controls;

namespace FlashMemo.View
{
    interface IViewFor<TViewModel>
    {
        public void SetVM(TViewModel vm);
    }
    public partial class MainWindow: Window
    {
        private MainVM mainVM;
        internal MainWindow(MainVM vm)
        {
            mainVM = vm;
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