using System.Windows;
using FlashMemo.ViewModel.Windows;

namespace FlashMemo.View
{
    public partial class BrowseWindow : Window, IViewFor<BrowseVM>
    {
        public BrowseVM VM { get; set; } = null!;
        public BrowseWindow()
        {
            InitializeComponent();
        }
    }
}
