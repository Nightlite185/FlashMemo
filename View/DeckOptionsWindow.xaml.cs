using FlashMemo.ViewModel.Windows;
using System.Windows;

namespace FlashMemo.View
{
    public partial class DeckOptionsWindow : Window, IViewFor<DeckOptionsMenuVM>
    {
        public DeckOptionsMenuVM VM { get; set; } = null!;
        public DeckOptionsWindow()
        {
            InitializeComponent();
        }
    }
}