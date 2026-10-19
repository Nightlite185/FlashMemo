using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using Syncfusion.UI.Xaml.TreeView;
using Syncfusion.UI.Xaml.TreeView.Engine;
using FlashMemo.ViewModel.Windows;
using FlashMemo.ViewModel.Wrappers;
using System.Collections.ObjectModel;

namespace FlashMemo.View;
public partial class DecksUC: UserControl
{
    public DecksUC()
    {
        InitializeComponent();
    }

    private void DeckTreeItem_Loaded(object sender, RoutedEventArgs e)
    {
        if (sender is not Syncfusion.UI.Xaml.TreeView.TreeViewItem item)
            return;

        // Syncfusion hard-codes both hover and drop colours in its default template.
        if (item.Template.FindName("PART_HoverBorder", item) is Border hover)
        {
            hover.SetResourceReference(Border.BackgroundProperty, "DeckRowHoverBrush");
        }

        if (item.Template.FindName("ROOT", item) is FrameworkElement root)
        {
            var dropState = VisualStateManager.GetVisualStateGroups(root)
                .OfType<VisualStateGroup>()
                .SelectMany(group => group.States.OfType<VisualState>())
                .FirstOrDefault(state => state.Name == "DropAsChild");

            if (dropState is not null)
                dropState.Storyboard = (Storyboard)DeckTree.FindResource("DeckDropTargetStoryboard");
        }
    }

    private void DeckRow_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ClickCount != 2 ||
            e.OriginalSource is not DependencyObject source ||
            FindAncestor<ButtonBase>(source) is not null ||
            FindAncestor<TextBox>(source) is not null ||
            sender is not FrameworkElement { DataContext: DeckNode deck } ||
            DataContext is not DecksVM vm)
            return;

        vm.ShowReviewCommand.Execute(deck);
        e.Handled = true;
    }

    private void DeckCtxButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not Button { Parent: Grid row } ||
            row.DataContext is not DeckNode deck ||
            row.ContextMenu is not ContextMenu menu)
            return;

        PrepareContextSelection(deck);
        menu.PlacementTarget = row;
        menu.IsOpen = true;
    }

    private void DeckRow_PreviewMouseRightButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (sender is FrameworkElement { DataContext: DeckNode deck })
            PrepareContextSelection(deck);
    }

    private void PrepareContextSelection(DeckNode deck)
    {
        // Opening a selected row's menu preserves the whole multi-selection.
        if (DeckTree.SelectedItems.Contains(deck))
            return;

        DeckTree.SelectedItems.Clear();
        DeckTree.SelectedItems.Add(deck);
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
        if (DataContext is not DecksVM vm)
            return;

        if (e.Key != Key.F2 ||
            vm.SelectedDecks.Count != 1 ||
            vm.SelectedDecks[0] is not DeckNode { IsRenaming: false } selectedDeck)
        {
            return;
        }

        selectedDeck.BeginRenameCommand.Execute(null);
        e.Handled = true;
    }

    private void Root_MouseDown(object sender, MouseButtonEventArgs e)
    {
        if (e.OriginalSource is DependencyObject source &&
            FindAncestor<TextBox>(source) is not null)
        {
            return;
        }

        Keyboard.ClearFocus();
    }

    private static T? FindAncestor<T>(DependencyObject source) where T : DependencyObject
    {
        DependencyObject? current = source;

        while (current is not null)
        {
            if (current is T match)
                return match;

            current = VisualTreeHelper.GetParent(current);
        }

        return null;
    }

    private void DeckTree_ItemDragStarting(object sender, TreeViewItemDragStartingEventArgs e)
    {
        // Reparenting currently moves one deck (including its subtree) at a time.
        e.Cancel = e.DraggingNodes.Count != 1;
    }

    private void DeckTree_ItemDragOver(object sender, TreeViewItemDragOverEventArgs e)
    {
        var request = CreateReparentRequest(e.DraggingNodes, e.TargetNode);
        e.DropPosition = ReferenceEquals(e.DragSource, DeckTree) &&
            DataContext is DecksVM vm &&
            vm.ReparentDeckCommand.CanExecute(request)
                ? e.TargetNode is null ? DropPosition.DropHere : DropPosition.DropAsChild
                : DropPosition.None;
    }

    // syncfusion's drag event handler, works for dropping on other decks.
    private async void DeckTree_ItemDropping(object sender, TreeViewItemDroppingEventArgs e)
    {
        e.Handled = true;
        var request = CreateReparentRequest(e.DraggingNodes, e.TargetNode);

        if (ReferenceEquals(e.DragSource, DeckTree) &&
            DataContext is DecksVM vm &&
            vm.ReparentDeckCommand.CanExecute(request))
        {
            await vm.ReparentDeckCommand.ExecuteAsync(request);
        }
    }

    // wpf's drag event handler. Only for dropping in empty space; moving to root.
    private async void DeckTree_Drop(object sender, DragEventArgs e)
    {
        // A row drop can change the layout before bubbling here. Its original
        // source still identifies it as a row drop, even if hit-testing no longer does.
        if (e.OriginalSource is DependencyObject source &&
            FindAncestor<Syncfusion.UI.Xaml.TreeView.TreeViewItem>(source) is not null)
            return;

        if (DeckTree.GetNodeAt(e.GetPosition(DeckTree)) is not null ||
            !ReferenceEquals(e.Data.GetData("Source"), DeckTree) ||
            e.Data.GetData("Nodes") is not ObservableCollection<TreeViewNode> nodes ||
            DataContext is not DecksVM vm)
            return;

        var request = CreateReparentRequest(nodes, targetNode: null);
        if (!vm.ReparentDeckCommand.CanExecute(request))
            return;

        e.Handled = true;
        e.Effects = DragDropEffects.Move;
        await vm.ReparentDeckCommand.ExecuteAsync(request);
    }

    private static DecksVM.DeckReparentRequest? CreateReparentRequest(
        ObservableCollection<TreeViewNode>? draggingNodes, TreeViewNode? targetNode)
    {
        return draggingNodes?.Count == 1 && draggingNodes[0].Content is DeckNode deck
            ? new(deck, targetNode?.Content as DeckNode)
            : null;
    }
}
