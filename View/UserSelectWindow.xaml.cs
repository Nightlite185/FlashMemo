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
            CreateButton.IsEnabled = false;
        }

        private async void User_DoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (sender is not ListBoxItem item ||
                item.DataContext is not UserVM user ||
                user.IsRenaming) return;

            await VM.LoginCommand.ExecuteAsync(user);
        }

        private async void RenameBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (sender is TextBox tb &&
                tb.DataContext is UserVM user)
            {
                if (e.Key == Key.Escape || string.IsNullOrWhiteSpace(user.TempName))
                {
                    user.CancelRenameCommand.Execute(null);
                }

                else if (e.Key == Key.Enter && !IsNameConflicting(user.TempName))
                {
                    await user.CommitRenameCommand.ExecuteAsync(null);
                }
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

        private async void RemoveButton_Click(object sender, RoutedEventArgs e)
        {
            //TODO: make this invisible for the currently logged user

            // TODO: persist user preference if they want to have this popup show or not.
            // I mean like "dont show this again" checkbox pattern

            var answer = MessageBox.Show(
                messageBoxText: "Are you sure you want to delete this user? This action can't be undone, and will delete every deck, card, etc. related with this user! Do you still wish to proceed?",
                
                caption: "Are you sure?",
                button: MessageBoxButton.YesNo,
                icon: MessageBoxImage.Warning
            );

            if (answer == MessageBoxResult.Yes)
            {
                if (sender is not Button bt) return;

                if (bt.DataContext is not UserVM user)
                    throw new InvalidOperationException(
                    $"Wrong button's datacontext type, its {bt.DataContext.GetType().FullName}");

                await VM.RemoveUserCommand.ExecuteAsync(user);
            }
        }

        private void CreateButton_Click(object sender, RoutedEventArgs e)
        {
            string name = VM.NewUsernameField;

            if (IsNameConflicting(name))
                return;

            VM.CreateUserCommand.ExecuteAsync(name);

            VM.NewUsernameField = "";
            Keyboard.ClearFocus();
        }

        private void NewUserNameBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                CreateButton_Click(sender, new());
            }

            else if (e.Key == Key.Escape)
                Keyboard.ClearFocus();
        }

        private bool IsNameConflicting(string name)
        {
            if (!VM.IsNameAvailable(name))
            {
                MessageBox.Show("A user with this name already exists, try picking another one.",
                "Name conflict",
                MessageBoxButton.OK,
                MessageBoxImage.Information);

                return true;
            }

            return false;
        }

        private void NewUserNameBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            CreateButton.IsEnabled = !string
                .IsNullOrWhiteSpace(NewUserNameBox.Text);
        }

        private async void UserList_KeyDown(object sender, KeyEventArgs e)
        {
            if (UsersList.SelectedItem is not UserVM user
            || user.IsRenaming) return;

            int idx = UsersList.SelectedIndex;

            switch (e.Key)
            {
                case Key.Up:
                    UsersList.SelectedIndex = (idx == 0)
                        ? idx
                        : idx--;
                    break;

                case Key.Down:
                    UsersList.SelectedIndex = (idx == UsersList.Items.Count-1)
                        ? idx
                        : idx++;
                    break;

                case Key.Enter:
                    await VM.LoginCommand
                        .ExecuteAsync(user);
                    break;
            };
        }
    }
}