using System.Windows;
using FlashMemo.ViewModel.Windows;

namespace FlashMemo.View
{
    public partial class UserOptionsWindow : Window, IViewFor<UserOptionsVM>
    {
        public UserOptionsVM VM { get; set; } = null!;
        public UserOptionsWindow()
        {
            InitializeComponent();
        }
    }
}
