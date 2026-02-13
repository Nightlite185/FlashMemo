using FlashMemo.ViewModel.Factories;
using FlashMemo.ViewModel.Popups;
using FlashMemo.ViewModel.Windows;
using FlashMemo.ViewModel.Wrappers;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

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
        if (sender is not TreeViewItem item
        || item.DataContext is not DeckNode deck
        || this.DataContext is not DeckSelectVM vm)
            return;

        await vm.ConfirmCommand.ExecuteAsync(deck);
    }
}