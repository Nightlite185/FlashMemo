using System.Windows;
using FlashMemo.ViewModel.Windows;

namespace FlashMemo.View
{
    public partial class EditCardWindow : Window, IViewFor<EditCardVM>
    {
        public EditCardVM VM { get; set; } = null!;
        public EditCardWindow()
        {
            InitializeComponent();
        }
    }
}
