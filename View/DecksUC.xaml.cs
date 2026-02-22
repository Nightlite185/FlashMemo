using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
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
}