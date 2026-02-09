using System.Windows;
using FlashMemo.ViewModel.Windows;

namespace FlashMemo.View
{
    public partial class UserSelectWindow: Window, IViewFor<UserSelectVM>
    {
        public UserSelectVM VM { get; set; } = null!;
        public UserSelectWindow()
        {
            InitializeComponent();
        }

        public void SetVM(UserSelectVM vm)
            => VM = vm;
    }
}
