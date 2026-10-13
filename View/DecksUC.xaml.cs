using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using GongSolutions.Wpf.DragDrop;
using FlashMemo.ViewModel.Windows;
using FlashMemo.ViewModel.Wrappers;

namespace FlashMemo.View;
public partial class DecksUC: UserControl, IDropTarget
{
    private DeckNode? selectionAnchor;
    private bool isPointerSelection;

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
        if (isPointerSelection ||
            sender is not TreeView treeView ||
            treeView.DataContext is not DecksVM vm ||
            e.NewValue is not DeckNode selected)
            return;

        vm.SelectDecks([selected]);
        selectionAnchor = selected;
    }

    private void DeckTree_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (sender is not TreeView treeView ||
            treeView.DataContext is not DecksVM vm ||
            e.OriginalSource is not DependencyObject source)
            return;

        TreeViewItem? item = FindAncestor<TreeViewItem>(source);

        if (item?.DataContext is not DeckNode deck)
        {
            if ((Keyboard.Modifiers & (ModifierKeys.Control | ModifierKeys.Shift)) == ModifierKeys.None)
            {
                vm.ClearSelection();
                selectionAnchor = null;
            }

            return;
        }

        if (IsInsideButtonBase(source) || IsInsideTextBox(source))
            return;

        isPointerSelection = true;

        bool isControlPressed = Keyboard.Modifiers.HasFlag(ModifierKeys.Control);
        bool isShiftPressed = Keyboard.Modifiers.HasFlag(ModifierKeys.Shift);

        if (isShiftPressed && selectionAnchor is not null)
        {
            vm.SelectDecks(
                GetVisibleRange(vm.DeckTree, selectionAnchor, deck),
                preserveExisting: isControlPressed);
        }
        else if (isControlPressed)
        {
            bool isBeingDeselected = deck.IsSelected;
            vm.ToggleDeckSelection(deck);

            // Prevent TreeView's built-in single selection from leaving a
            // deselected item visually selected.
            if (isBeingDeselected)
            {
                item.IsSelected = false;
                e.Handled = true;
            }
        }
        else
        {
            vm.SelectDecks([deck]);
        }

        selectionAnchor = deck;
        Dispatcher.BeginInvoke(() => isPointerSelection = false);
    }

    private void DeckCtxButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not Button button ||
            FindAncestor<TreeViewItem>(button) is not TreeViewItem item ||
            item.DataContext is not DeckNode deck ||
            DataContext is not DecksVM vm ||
            item.ContextMenu is not ContextMenu menu)
            return;

        PrepareContextSelection(item, deck, vm);
        menu.PlacementTarget = item;
        menu.IsOpen = true;
    }

    private void TreeViewItem_PreviewMouseRightButtonDown(
        object sender,
        MouseButtonEventArgs e)
    {
        if (sender is not TreeViewItem item ||
            e.OriginalSource is not DependencyObject source ||
            !ReferenceEquals(FindAncestor<TreeViewItem>(source), item) ||
            item.DataContext is not DeckNode deck ||
            DataContext is not DecksVM vm)
            return;

        PrepareContextSelection(item, deck, vm);

        if (item.ContextMenu is ContextMenu menu)
            menu.PlacementTarget = item;
    }

    private void PrepareContextSelection(
        TreeViewItem item,
        DeckNode deck,
        DecksVM vm)
    {
        if (!deck.IsSelected)
            vm.SelectDecks([deck]);

        isPointerSelection = true;
        item.IsSelected = true;
        selectionAnchor = deck;
        Dispatcher.BeginInvoke(() => isPointerSelection = false);
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

        if (e.Key == Key.A && Keyboard.Modifiers.HasFlag(ModifierKeys.Control))
        {
            vm.SelectDecks(GetVisibleDecks(vm.DeckTree));
            e.Handled = true;
            return;
        }

        if (e.Key != Key.F2 ||
            vm.SelectedDecks.Count != 1 ||
            vm.SelectedDecks[0].IsRenaming)
        {
            return;
        }

        DeckNode selectedDeck = vm.SelectedDecks[0];
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

    private static IEnumerable<DeckNode> GetVisibleRange(
        IEnumerable<DeckNode> roots,
        DeckNode first,
        DeckNode second)
    {
        List<DeckNode> visibleDecks = [.. GetVisibleDecks(roots)];
        int firstIndex = visibleDecks.IndexOf(first);
        int secondIndex = visibleDecks.IndexOf(second);

        if (firstIndex < 0 || secondIndex < 0)
            return [second];

        int start = Math.Min(firstIndex, secondIndex);
        int count = Math.Abs(firstIndex - secondIndex) + 1;
        return visibleDecks.GetRange(start, count);
    }

    private static IEnumerable<DeckNode> GetVisibleDecks(IEnumerable<DeckNode> nodes)
    {
        foreach (DeckNode node in nodes)
        {
            yield return node;

            if (!node.IsExpanded)
                continue;

            foreach (DeckNode child in GetVisibleDecks(node.Children))
                yield return child;
        }
    }

    void IDropTarget.DragOver(IDropInfo dropInfo)
    {
        if (!TryCreateReparentRequest(dropInfo, out DecksVM.DeckReparentRequest? request) ||
            DataContext is not DecksVM vm ||
            !vm.ReparentDeckCommand.CanExecute(request))
        {
            dropInfo.Effects = DragDropEffects.None;
            return;
        }

        dropInfo.DropTargetAdorner = DropTargetAdorners.Highlight;
        dropInfo.Effects = DragDropEffects.Move;
    }

    async void IDropTarget.Drop(IDropInfo dropInfo)
    {
        if (!TryCreateReparentRequest(dropInfo, out DecksVM.DeckReparentRequest? request) ||
            DataContext is not DecksVM vm ||
            !vm.ReparentDeckCommand.CanExecute(request))
        {
            return;
        }

        await vm.ReparentDeckCommand.ExecuteAsync(request);
    }

    private static bool TryCreateReparentRequest(IDropInfo dropInfo, out DecksVM.DeckReparentRequest? request)
    {
        request = null;

        if (dropInfo.Data is not DeckNode draggedDeck)
            return false;

        DeckNode? targetDeck = dropInfo.TargetItem as DeckNode;
        request = new DecksVM.DeckReparentRequest(draggedDeck, targetDeck);
        return true;
    }

    // TODO: Add export ctx menu option and wire it with a popup connected with a fresh instance of ExportVM.
}
