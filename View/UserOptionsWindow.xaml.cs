using System.Windows;
using FlashMemo.ViewModel.Windows;

namespace FlashMemo.View
{
    public partial class UserOptionsWindow : Window, IViewFor<UserOptionsMenuVM>
    {
        public UserOptionsMenuVM VM { get; set; } = null!;
        public UserOptionsWindow()
        {
            InitializeComponent();
        }
    }
}