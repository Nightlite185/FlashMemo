using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using FlashMemo.ViewModel.Windows;
using FlashMemo.ViewModel.Wrappers;

namespace FlashMemo.View;
public partial class DecksUC: UserControl
{
    public DecksUC()
    {
        InitializeComponent();
    }

    private void TreeViewItem_DoubleClick(object sender, MouseButtonEventArgs e)
    {
        if (e.OriginalSource is DependencyObject source 
        && IsInsideButtonBase(source))
        {
            e.Handled = true;
            return;
        }

        if (sender is not TreeViewItem item
        || item.DataContext is not DeckNode deck
        || this.DataContext is not DecksVM vm)
            return;

        vm.ShowReviewCommand.Execute(deck);
    }

    private void DeckTree_SelectedChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
    {
        if (sender is TreeView treeView &&
            treeView.DataContext is DecksVM vm &&
            e.NewValue is DeckNode selected)
        {
            vm.SelectedDeck = selected;
        }
    }

    private void DeckCtxButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not Button btn || btn.ContextMenu is null)
            return;

        btn.ContextMenu.PlacementTarget = btn;
        btn.ContextMenu.IsOpen = true;
    }

    private async void RenameBox_KeyDown(object sender, KeyEventArgs e)
    {
        if (sender is not TextBox tb ||
            tb.DataContext is not DeckNode deck ||
            DataContext is not DecksVM vm)
        {
            return;
        }

        if (e.Key == Key.Escape)
        {
            deck.CancelRenameCommand.Execute(null);
            e.Handled = true;
            return;
        }

        if (e.Key != Key.Enter)
            return;

        if (string.IsNullOrWhiteSpace(deck.TempName))
        {
            deck.CancelRenameCommand.Execute(null);
        }
        else
        {
            await vm.RenameDeckCommand.ExecuteAsync(deck);
        }

        e.Handled = true;
    }

    private void RenameBox_LostFocus(object sender, RoutedEventArgs e)
    {
        if (sender is TextBox tb &&
            tb.DataContext is DeckNode deck)
        {
            Keyboard.ClearFocus();
            deck.CancelRenameCommand.Execute(null);
        }
    }

    private void RenameBox_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        if (sender is not TextBox tb || !tb.IsVisible)
            return;

        tb.Dispatcher.BeginInvoke(() =>
        {
            tb.Focus();
            Keyboard.Focus(tb);
            tb.SelectAll();
        });
    }

    private void DeckTree_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key != Key.F2 ||
            DataContext is not DecksVM vm ||
            vm.SelectedDeck is not DeckNode selectedDeck ||
            selectedDeck.IsRenaming)
        {
            return;
        }

        selectedDeck.BeginRenameCommand.Execute(null);
        e.Handled = true;
    }

    private void Root_MouseDown(object sender, MouseButtonEventArgs e)
    {
        if (e.OriginalSource is DependencyObject source &&
            IsInsideTextBox(source))
        {
            return;
        }

        Keyboard.ClearFocus();
    }

    private static bool IsInsideTextBox(DependencyObject source)
    {
        DependencyObject? current = source;

        while (current is not null)
        {
            if (current is TextBox)
                return true;

            current = VisualTreeHelper.GetParent(current);
        }

        return false;
    }

    private static bool IsInsideButtonBase(DependencyObject source)
    {
        DependencyObject? current = source;

        while (current is not null)
        {
            if (current is ButtonBase)
                return true;

            current = VisualTreeHelper.GetParent(current);
        }

        return false;
    }
}
