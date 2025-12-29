//using FlashMemo.ViewModel;
//using System.Windows;
//using System.Windows.Controls;

//namespace FlashMemo
//{
//    public partial class MainWindow : Window
//    {
//        private readonly MainVM mainVM;
//        public MainWindow()
//        {
//            InitializeComponent();
//            mainVM = new();
//            this.DataContext = mainVM;
//        }

//        private void DeckOptions_Click(object sender, RoutedEventArgs e)
//        {
//            if (sender is Button bt && bt.ContextMenu != null)
//            {
//                bt.ContextMenu.PlacementTarget = bt;
//                bt.ContextMenu.IsOpen = true;
//            }
//        }

//    }
//}