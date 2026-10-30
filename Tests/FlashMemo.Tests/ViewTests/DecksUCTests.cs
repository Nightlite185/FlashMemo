using System.Collections.ObjectModel;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using FlashMemo.Model.Persistence;
using FlashMemo.Repositories;
using FlashMemo.Services;
using FlashMemo.View;
using FlashMemo.ViewModel.Windows;
using FlashMemo.ViewModel.Wrappers;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Syncfusion.UI.Xaml.TreeView;
using Syncfusion.UI.Xaml.TreeView.Engine;
using SfTreeViewItem = Syncfusion.UI.Xaml.TreeView.TreeViewItem;

namespace FlashMemo.Tests.ViewTests;

[Collection("WPF")]
public class DecksUCTests(WpfTestHost host)
{
    [Fact]
    public Task Tree_AllowsDraggingAndReceivingDrops() => host.Run(() =>
    {
        var (view, vm, _) = CreateView();
        var tree = Tree(view);

        tree.AllowDragging.Should().BeTrue();
        tree.AllowDrop.Should().BeTrue();
        tree.SelectedItems.Should().BeSameAs(vm.SelectedDecks);
    });

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public Task RightClick_PreservesSelectedRowsOrSelectsOnlyTheUnselectedTarget(bool targetAlreadySelected)
        => host.Run(() =>
    {
        var (view, vm, repo) = CreateView();
        var first = vm.DeckTree[0];
        var second = vm.DeckTree[1];
        var other = new DeckNode(new Deck { Id = 3, UserId = 1, Name = "Other" }, []);
        vm.DeckTree.Add(other);
        vm.SelectedDecks.Add(first);
        vm.SelectedDecks.Add(second);
        Layout(view);
        var target = targetAlreadySelected ? first : other;
        var row = Descendants(Tree(view)).OfType<Grid>()
            .Single(grid => ReferenceEquals(grid.DataContext, target) && grid.ContextMenu is not null);

        row.RaiseEvent(new MouseButtonEventArgs(Mouse.PrimaryDevice, Environment.TickCount, MouseButton.Right)
        {
            RoutedEvent = UIElement.PreviewMouseRightButtonDownEvent
        });

        if (targetAlreadySelected)
            vm.SelectedDecks.Should().Equal(first, second);
        else
            vm.SelectedDecks.Should().Equal(other);
        repo.VerifyNoOtherCalls();
    });

    [Fact]
    public Task NativeDrop_ReparentsRootAndPersistsItOnce() => host.Run(() =>
    {
        var (view, vm, repo) = CreateView();
        var tree = Tree(view);
        DeckNode deck = vm.DeckTree[0];
        DeckNode parent = vm.DeckTree[1];

        Drop(tree, tree.Nodes[0], targetRow: 1);

        vm.DeckTree.Should().Equal(parent);
        parent.Children.Should().Equal(deck);
        deck.Parent.Should().BeSameAs(parent);
        parent.IsExpanded.Should().BeTrue();
        repo.Verify(r => r.SaveEditedDeck(It.Is<Deck>(d =>
            d.Id == deck.Id && d.ParentDeckId == parent.Id)), Times.Once);
        repo.VerifyNoOtherCalls();
    });

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public Task RoutedDrop_OnDeckRow_ReparentsAndSavesOnlyOnce(bool fromRowContent) => host.Run(() =>
    {
        var (view, vm, repo) = CreateView();
        var tree = Tree(view);
        DeckNode deck = vm.DeckTree[0];
        DeckNode parent = vm.DeckTree[1];
        var row = Descendants(tree).OfType<SfTreeViewItem>().ElementAt(1);
        row.RaiseEvent(new RoutedEventArgs(FrameworkElement.LoadedEvent));
        var point = row.TranslatePoint(new Point(row.ActualWidth / 2, row.ActualHeight / 2), tree);
        var args = CreateDragArgs(tree, tree.Nodes[0], tree, point);
        args.RoutedEvent = UIElement.DropEvent;

        UIElement source = fromRowContent ? Descendants(row).OfType<TextBlock>().First() : row;
        source.RaiseEvent(args);

        deck.Parent.Should().BeSameAs(parent);
        vm.DeckTree.Should().Equal(parent);
        parent.Children.Should().Equal(deck);
        repo.Verify(r => r.SaveEditedDeck(It.Is<Deck>(d =>
            d.Id == deck.Id && d.ParentDeckId == parent.Id)), Times.Once);
        repo.VerifyNoOtherCalls();
    });

    [Fact]
    public Task NativeDrop_OnEmptySpace_MovesChildBackToRoot() => host.Run(() =>
    {
        var (view, vm, repo) = CreateView();
        var tree = Tree(view);
        DeckNode parent = vm.DeckTree[0];
        DeckNode child = new(new Deck { Id = 3, UserId = 1, Name = "Child" }, []);
        parent.AddChild(child);
        parent.IsExpanded = true;
        Layout(view);

        Drop(tree, tree.Nodes[0].ChildNodes[0], targetRow: -1);

        parent.Children.Should().BeEmpty();
        child.Parent.Should().BeNull();
        vm.DeckTree.Should().Contain(child);
        repo.Verify(r => r.SaveEditedDeck(It.Is<Deck>(d =>
            d.Id == child.Id && d.ParentDeckId == null)), Times.Once);
        repo.VerifyNoOtherCalls();
    });

    [Fact]
    public Task RoutedDrop_BelowDecks_MovesChildSubtreeToRootAndPersistsNullParent() => host.Run(() =>
    {
        var (view, vm, repo) = CreateView();
        var tree = Tree(view);
        DeckNode parent = vm.DeckTree[0];
        DeckNode child = new(new Deck { Id = 3, UserId = 1, Name = "Child" }, []);
        DeckNode grandchild = new(new Deck { Id = 4, UserId = 1, Name = "Grandchild" }, []);
        parent.AddChild(child);
        child.AddChild(grandchild);
        parent.IsExpanded = true;
        Layout(view);
        var emptyPoint = new Point(tree.ActualWidth / 2, tree.ActualHeight - 8);
        tree.GetNodeAt(emptyPoint).Should().BeNull();
        var args = CreateDragArgs(tree, tree.Nodes[0].ChildNodes[0], tree, emptyPoint);
        args.RoutedEvent = UIElement.DropEvent;
        args.GetPosition(tree).Should().Be(emptyPoint);

        tree.RaiseEvent(args);

        parent.Children.Should().BeEmpty();
        child.Parent.Should().BeNull();
        vm.DeckTree.Should().ContainSingle(deck => ReferenceEquals(deck, child));
        child.Children.Should().Equal(grandchild);
        grandchild.Parent.Should().BeSameAs(child);
        repo.Verify(r => r.SaveEditedDeck(It.Is<Deck>(d =>
            d.Id == child.Id && d.ParentDeckId == null)), Times.Once);
        repo.VerifyNoOtherCalls();
    });

    [Fact]
    public Task RoutedDrop_BelowDecks_WhenAlreadyRoot_DoesNotDuplicateOrSave() => host.Run(() =>
    {
        var (view, vm, repo) = CreateView();
        var tree = Tree(view);
        DeckNode deck = vm.DeckTree[0];
        var args = CreateDragArgs(tree, tree.Nodes[0], tree,
            new Point(tree.ActualWidth / 2, tree.ActualHeight - 8));
        args.RoutedEvent = UIElement.DropEvent;

        tree.RaiseEvent(args);

        deck.Parent.Should().BeNull();
        vm.DeckTree.Should().HaveCount(2);
        vm.DeckTree.Should().ContainSingle(root => ReferenceEquals(root, deck));
        repo.VerifyNoOtherCalls();
    });

    [Fact]
    public Task RoutedDrop_BelowDecks_FromAnotherTree_IsIgnored() => host.Run(() =>
    {
        var (view, vm, repo) = CreateView();
        var tree = Tree(view);
        DeckNode parent = vm.DeckTree[0];
        DeckNode child = new(new Deck { Id = 3, UserId = 1, Name = "Child" }, []);
        parent.AddChild(child);
        parent.IsExpanded = true;
        Layout(view);
        var args = CreateDragArgs(tree, tree.Nodes[0].ChildNodes[0], new SfTreeView(),
            new Point(tree.ActualWidth / 2, tree.ActualHeight - 8));
        args.RoutedEvent = UIElement.DropEvent;

        tree.RaiseEvent(args);

        child.Parent.Should().BeSameAs(parent);
        parent.Children.Should().Equal(child);
        vm.DeckTree.Should().HaveCount(2);
        repo.VerifyNoOtherCalls();
    });

    [Fact]
    public Task NativeDrop_OnDescendant_RejectsCycleWithoutSaving() => host.Run(() =>
    {
        var (view, vm, repo) = CreateView();
        var tree = Tree(view);
        DeckNode parent = vm.DeckTree[0];
        DeckNode child = new(new Deck { Id = 3, UserId = 1, Name = "Child" }, []);
        parent.AddChild(child);
        parent.IsExpanded = true;
        Layout(view);

        Drop(tree, tree.Nodes[0], targetRow: 1);

        parent.Parent.Should().BeNull();
        parent.Children.Should().Equal(child);
        child.Parent.Should().BeSameAs(parent);
        vm.DeckTree.Should().HaveCount(2);
        repo.VerifyNoOtherCalls();
    });

    [Fact]
    public Task NativeDrop_FromAnotherTree_IsIgnored() => host.Run(() =>
    {
        var (view, vm, repo) = CreateView();
        var tree = Tree(view);

        Drop(tree, tree.Nodes[0], targetRow: 1, source: new SfTreeView());

        vm.DeckTree.Should().HaveCount(2);
        vm.DeckTree.Should().OnlyContain(deck => deck.Parent == null && deck.Children.Count == 0);
        repo.VerifyNoOtherCalls();
    });

    [Fact]
    public Task LoadedRow_UsesSubtleHoverAndKeepsSelectionColour() => host.Run(() =>
    {
        var (view, _, _) = CreateView();
        var tree = Tree(view);
        SfTreeViewItem row = Descendants(tree).OfType<SfTreeViewItem>().First();
        row.RaiseEvent(new RoutedEventArgs(FrameworkElement.LoadedEvent));
        var hover = (Border)row.Template.FindName("PART_HoverBorder", row);

        ((SolidColorBrush)hover.Background).Color.Should().Be(Color.FromRgb(42, 42, 42));
        ((SolidColorBrush)tree.SelectionBackgroundColor).Color.Should().Be(Color.FromArgb(64, 42, 130, 218));
    });

    [Fact]
    public Task DragPreview_IsCompactTranslucentIconWithoutCounters() => host.Run(() =>
    {
        var (view, _, _) = CreateView();
        var preview = (FrameworkElement)Tree(view).DragPreviewTemplate.LoadContent();
        preview.Measure(new Size(900, 600));
        preview.Arrange(new Rect(preview.DesiredSize));
        var outline = Descendants(preview).OfType<System.Windows.Shapes.Rectangle>().Single();

        preview.ActualWidth.Should().BeLessThan(50);
        preview.ActualHeight.Should().BeLessThan(50);
        Descendants(preview).OfType<TextBlock>().Should().BeEmpty();
        ((SolidColorBrush)outline.Fill).Color.A.Should().BeInRange((byte)1, (byte)127);
        outline.RaiseEvent(new RoutedEventArgs(FrameworkElement.LoadedEvent));
        outline.HasAnimatedProperties.Should().BeTrue();
    });

    [Fact]
    public Task Expander_UsesHandCursorAndRotatesWhenExpanded() => host.Run(() =>
    {
        var (view, vm, _) = CreateView();
        vm.DeckTree[0].AddChild(new(new Deck { Id = 3, UserId = 1, Name = "Child" }, []));
        Layout(view);
        var tree = Tree(view);
        var row = Descendants(tree).OfType<SfTreeViewItem>().First();
        var expander = (ContentControl)row.Template.FindName("PART_ExpanderView", row);
        var glyph = Descendants(expander).OfType<System.Windows.Shapes.Path>().Single();

        Descendants(expander).OfType<Grid>().Should().Contain(grid => grid.Cursor == Cursors.Hand);
        ((SolidColorBrush)glyph.Stroke).Color.Should().Be(Color.FromRgb(154, 165, 177));
        glyph.RenderTransform.Value.IsIdentity.Should().BeTrue();

        tree.Nodes[0].IsExpanded = true;
        Layout(view);

        glyph.RenderTransform.Should().BeOfType<RotateTransform>().Subject.Angle.Should().Be(90);
        vm.DeckTree[0].IsExpanded.Should().BeTrue();
    });

    [Fact]
    public Task ActiveDropTarget_UsesHoverColourAndReturnsToNormalState() => host.Run(() =>
    {
        var (view, _, _) = CreateView();
        var row = Descendants(Tree(view)).OfType<SfTreeViewItem>().First();
        row.RaiseEvent(new RoutedEventArgs(FrameworkElement.LoadedEvent));
        var background = (Border)row.Template.FindName("BorderContent", row);
        var expander = (ContentControl)row.Template.FindName("PART_ExpanderView", row);

        VisualStateManager.GoToState(row, "DropAsChild", false).Should().BeTrue();
        Dispatcher.CurrentDispatcher.Invoke(() => { }, DispatcherPriority.Render);

        ((SolidColorBrush)background.Background).Color.Should().Be(Color.FromRgb(42, 42, 42));
        ((SolidColorBrush)background.BorderBrush).Color.Should().Be(Color.FromRgb(42, 42, 42));
        ((SolidColorBrush)expander.Background).Color.Should().Be(Color.FromRgb(42, 42, 42));

        VisualStateManager.GoToState(row, "Normal", false).Should().BeTrue();
        var root = (FrameworkElement)row.Template.FindName("ROOT", row);
        VisualStateManager.GetVisualStateGroups(root).OfType<VisualStateGroup>()
            .Single(group => group.Name == "DropStates").CurrentState.Name.Should().Be("Normal");
    });

    private static (DecksUC View, DecksVM VM, Mock<IDeckRepo> Repo) CreateView()
    {
        var repo = new Mock<IDeckRepo>(MockBehavior.Strict);
        repo.Setup(r => r.SaveEditedDeck(It.IsAny<Deck>())).Returns(Task.CompletedTask);
        var vm = new DecksVM(repo.Object, null!, null!, null!, null!, 1, null!, null!,
            NullLogger<DecksVM>.Instance, Mock.Of<IDialogService>(), null!, Helpers.GetSafetyNet());
        vm.DeckTree.Add(new(new Deck { Id = 1, UserId = 1, Name = "First" }, []));
        vm.DeckTree.Add(new(new Deck { Id = 2, UserId = 1, Name = "Second" }, []));
        var view = new DecksUC { DataContext = vm };
        Layout(view);
        return (view, vm, repo);
    }

    private static SfTreeView Tree(DecksUC view) => (SfTreeView)view.FindName("DeckTree");

    private static void Layout(FrameworkElement view)
    {
        view.Measure(new Size(900, 600));
        view.Arrange(new Rect(0, 0, 900, 600));
        view.UpdateLayout();
    }

    private static void Drop(SfTreeView tree, TreeViewNode dragged, int targetRow, SfTreeView? source = null)
    {
        var args = CreateDragArgs(tree, dragged, source ?? tree, new Point(100, 80));
        var controller = new TestDropController();
        tree.DragDropController = controller;
        controller.Drop(args, targetRow);
    }

    private static DragEventArgs CreateDragArgs(SfTreeView tree, TreeViewNode dragged, SfTreeView source, Point point)
    {
        var data = new DataObject();
        data.SetData("Source", source);
        data.SetData("Nodes", new ObservableCollection<TreeViewNode> { dragged });
        // WPF exposes no public constructor for synthetic drag events. Exercise
        // Syncfusion's real drop controller with its documented data format.
        var constructor = typeof(DragEventArgs).GetConstructors(BindingFlags.Instance | BindingFlags.NonPublic)
            .Single(c => c.GetParameters().Length == 5);
        return (DragEventArgs)constructor.Invoke(
            [data, DragDropKeyStates.LeftMouseButton, DragDropEffects.Move, tree, point]);
    }

    private sealed class TestDropController : TreeViewDragDropController
    {
        public void Drop(DragEventArgs args, int row) => ProcessOnDrop(args, row);
    }

    private static IEnumerable<DependencyObject> Descendants(DependencyObject root)
    {
        for (int i = 0; i < VisualTreeHelper.GetChildrenCount(root); i++)
        {
            var child = VisualTreeHelper.GetChild(root, i);
            yield return child;
            foreach (var descendant in Descendants(child)) yield return descendant;
        }
    }
}
