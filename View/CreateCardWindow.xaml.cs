using System.Windows;
using FlashMemo.ViewModel.Windows;

namespace FlashMemo.View
{
    public partial class CreateCardWindow : Window, IViewFor<CreateCardVM>
    {
        public CreateCardVM VM { get; set; } = null!;
        public CreateCardWindow()
        {
            InitializeComponent();
        }
    }
}
