using FlashMemo.ViewModel.Popups;
using FlashMemo.ViewModel.Wrappers;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;

namespace FlashMemo.View;

public partial class DeckSelectUC : UserControl
{
    public DeckSelectUC()
    {
        InitializeComponent();

        DeckTree.SelectedItemChanged += OnSelectedChanged;
    }

    private void OnSelectedChanged(object sender,
        RoutedPropertyChangedEventArgs<object> e)
    {
        if (DataContext is DeckSelectVM vm)
        {
            vm.SelectedDeck = e.NewValue as DeckNode
                ?? throw new NullReferenceException();
        }
    }

    private async void DeckTree_KeyDown(object sender, KeyEventArgs e)
    {
        if (DataContext is not DeckSelectVM vm) 
            return;

        if (e.Key == Key.Enter)
            await vm.ConfirmCommand.ExecuteAsync(null);

        else if (e.Key == Key.Escape)
            vm.CloseCommand.Execute(null);
    }
    private async void TreeViewItem_DoubleClick(object sender, MouseButtonEventArgs e)
    {
        if (e.OriginalSource is DependencyObject source &&
            IsInsideButtonBase(source))
        {
            e.Handled = true;
            return;
        }

        if (sender is not TreeViewItem item
        || item.DataContext is not DeckNode deck
        || this.DataContext is not DeckSelectVM vm)
            return;

        await vm.ConfirmCommand.ExecuteAsync(deck);
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
