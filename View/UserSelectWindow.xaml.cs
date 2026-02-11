using FlashMemo.ViewModel.Windows;
using FlashMemo.ViewModel.Wrappers;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace FlashMemo.View
{
    public partial class UserSelectWindow: Window, IViewFor<UserSelectVM>
    {
        public UserSelectVM VM { get; set; } = null!;
        public UserSelectWindow()
        {
            InitializeComponent();
        }

        private void User_DoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (DataContext is UserSelectVM vm &&
                sender is ListBoxItem item &&
                item.DataContext is UserVM user && 
                !user.IsRenaming)
            {
                vm.LoginCommand.Execute(user);
            }
        }

        private async void RenameBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (sender is TextBox tb &&
                tb.DataContext is UserVM user)
            {
                if (e.Key == Key.Enter)
                    await user.CommitRenameCommand.ExecuteAsync(null);

                else if (e.Key == Key.Escape)
                    user.CancelRenameCommand.Execute(null);
            }
        }

        private void RenameBox_LostFocus(object sender, RoutedEventArgs e)
        {
            if (sender is TextBox tb &&
                tb.DataContext is UserVM user)
            {
                Keyboard.ClearFocus();
                user.CancelRenameCommand.Execute(null);
            }
        }


        public void SetVM(UserSelectVM vm)
            => VM = vm;

        private void ClearFocus(object sender, MouseButtonEventArgs e)
        {
            Keyboard.ClearFocus();
        }

        private void TextBox_Loaded(object sender, RoutedEventArgs e)
        {
            var tb = sender as TextBox 
                ?? throw new InvalidOperationException(
                "Wrong control type, expected TextBox.");

            tb.Focus();
            Keyboard.Focus(tb);
            tb.SelectAll();
        }        
    }
}