using FlashMemo.ViewModel;
using System.Windows;
using System.Windows.Controls;

namespace FlashMemo.View
{
    interface IViewFor<TViewModel>
    {
        public void SetVM(TViewModel vm);
    }
    public partial class MainWindow: Window, IViewFor<MainVM>
    {
        private MainVM vm = null!;
        public MainWindow()
        {
            InitializeComponent();
        }

        public void SetVM(MainVM vm)
        {
            this.DataContext = vm;
            this.vm = vm;
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