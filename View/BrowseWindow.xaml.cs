using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using FlashMemo.Model.Persistence;
using FlashMemo.ViewModel.Windows;

namespace FlashMemo.View;

public partial class BrowseWindow : Window, IViewFor<BrowseVM>
{
    public BrowseVM VM { get; set; } = null!;

    private readonly List<BrowseColumnSpec> columnSpecs = [];
    private readonly Dictionary<DataGridColumn, BrowseColumnSpec> specByColumn = [];

    private IValueConverter? xamlToTextConverter;
    private IValueConverter? enumToReadableConverter;

    private BrowseColumn activeSortColumn = BrowseColumn.Due;
    private ListSortDirection activeSortDirection = ListSortDirection.Ascending;
    private bool initialized;

    public BrowseWindow()
    {
        InitializeComponent();

        Loaded += OnLoaded;
        CardsGrid.Sorting += CardsGrid_Sorting;
        CardsGrid.ColumnReordered += CardsGrid_ColumnReordered;
        CardsGrid.SelectionChanged += CardsGrid_SelectionChanged;
        CardsGrid.PreviewMouseRightButtonDown += CardsGrid_PreviewMouseRightButtonDown;
        CardsGrid.PreviewMouseRightButtonUp += CardsGrid_PreviewMouseRightButtonUp;
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        EnsureInitialized();
    }

    private void EnsureInitialized()
    {
        if (initialized)
            return;

        xamlToTextConverter = TryFindResource("XamlToTextConverter") as IValueConverter;
        enumToReadableConverter = TryFindResource("EnumToReadableTextConverter") as IValueConverter;

        InitializeColumnSpecs();
        RebuildColumns();
        ApplySortToCollectionView();

        initialized = true;
    }

    private void InitializeColumnSpecs()
    {
        if (columnSpecs.Count > 0)
            return;

        int order = 0;
        columnSpecs.Add(new(BrowseColumn.NoteFrontContent, "Front content", "Note.FrontContent", order++, 260, converter: xamlToTextConverter, isVisible: true));
        columnSpecs.Add(new(BrowseColumn.DeckName, "Deck", "DeckName", order++, 220, isVisible: true));
        columnSpecs.Add(new(BrowseColumn.Due, "Due date", "Due", order++, 150, stringFormat: "yyyy-MM-dd HH:mm", isVisible: true));
        columnSpecs.Add(new(BrowseColumn.State, "State", "State", order++, 110, converter: enumToReadableConverter, isVisible: true));
        columnSpecs.Add(new(BrowseColumn.DayInterval, "Interval (days)", "DayInterval", order++, 120, isVisible: true));
        columnSpecs.Add(new(BrowseColumn.Created, "Created", "Created", order++, 170, stringFormat: "yyyy-MM-dd HH:mm", isVisible: true));
        columnSpecs.Add(new(BrowseColumn.NoteType, "Note type", "Note.Type", order++, 120, converter: enumToReadableConverter));
        columnSpecs.Add(new(BrowseColumn.NoteBackContent, "Back content", "Note.BackContent", order++, 260, converter: xamlToTextConverter));
        columnSpecs.Add(new(BrowseColumn.Id, "Id", "Id", order++, 120));
        columnSpecs.Add(new(BrowseColumn.LastModified, "Last modified", "LastModified", order++, 170, stringFormat: "yyyy-MM-dd HH:mm"));
        columnSpecs.Add(new(BrowseColumn.LearningStage, "Learning stage", "LearningStage", order++, 130));
        columnSpecs.Add(new(BrowseColumn.IsBuried, "Buried", "IsBuried", order++, 110));
        columnSpecs.Add(new(BrowseColumn.IsSuspended, "Suspended", "IsSuspended", order++, 125));
        columnSpecs.Add(new(BrowseColumn.Tags, "Tags", "TagsDisplay", order++, 220));
    }

    private void RebuildColumns()
    {
        CaptureColumnLayout();

        CardsGrid.Columns.Clear();
        specByColumn.Clear();

        var visibleSpecs = columnSpecs
            .Where(spec => spec.IsVisible)
            .OrderBy(spec => spec.Order)
            .ToList();

        foreach (var spec in visibleSpecs)
        {
            var col = CreateColumn(spec);
            CardsGrid.Columns.Add(col);
            specByColumn[col] = spec;
        }

        RefreshColumnHeaders();
    }

    private void CaptureColumnLayout()
    {
        if (specByColumn.Count == 0)
            return;

        int order = 0;
        var visible = specByColumn
            .OrderBy(pair => pair.Key.DisplayIndex)
            .ToList();

        foreach (var pair in visible)
        {
            pair.Value.Order = order++;
            pair.Value.Width = pair.Key.Width;
        }

        foreach (var hidden in columnSpecs.Where(spec => !spec.IsVisible).OrderBy(spec => spec.Order))
            hidden.Order = order++;
    }

    private DataGridTextColumn CreateColumn(BrowseColumnSpec spec)
    {
        var binding = new Binding(spec.BindingPath)
        {
            Mode = BindingMode.OneWay
        };

        if (spec.StringFormat is not null)
            binding.StringFormat = spec.StringFormat;

        if (spec.Converter is not null)
            binding.Converter = spec.Converter;

        return new DataGridTextColumn
        {
            Header = spec.Header,
            Binding = binding,
            SortMemberPath = spec.SortMemberPath,
            Width = spec.Width,
            MinWidth = 90,
            ElementStyle = (Style)FindResource("BrowseCellTextStyle")
        };
    }

    private void RefreshColumnHeaders()
    {
        foreach (var pair in specByColumn)
        {
            var spec = pair.Value;
            pair.Key.Header = BuildHeaderContent(
                spec.Header,
                spec.Column == activeSortColumn
                    ? (activeSortDirection == ListSortDirection.Ascending ? "^" : "v")
                    : string.Empty);
        }
    }

    private static Grid BuildHeaderContent(string title, string sortGlyph)
    {
        var grid = new Grid
        {
            HorizontalAlignment = HorizontalAlignment.Stretch
        };

        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

        var titleText = new TextBlock
        {
            Text = title,
            VerticalAlignment = VerticalAlignment.Center,
            Foreground = new SolidColorBrush(Color.FromRgb(142, 201, 255))
        };

        Grid.SetColumn(titleText, 0);
        grid.Children.Add(titleText);

        var glyphText = new TextBlock
        {
            Text = sortGlyph,
            Width = 14,
            HorizontalAlignment = HorizontalAlignment.Right,
            VerticalAlignment = VerticalAlignment.Center,
            Foreground = new SolidColorBrush(Color.FromRgb(142, 201, 255))
        };

        Grid.SetColumn(glyphText, 1);
        grid.Children.Add(glyphText);

        return grid;
    }

    private void CardsGrid_Sorting(object sender, DataGridSortingEventArgs e)
    {
        e.Handled = true;

        if (!specByColumn.TryGetValue(e.Column, out var clickedSpec))
            return;

        if (activeSortColumn == clickedSpec.Column)
        {
            activeSortDirection = activeSortDirection == ListSortDirection.Ascending
                ? ListSortDirection.Descending
                : ListSortDirection.Ascending;
        }
        else
        {
            activeSortColumn = clickedSpec.Column;
            activeSortDirection = ListSortDirection.Ascending;
        }

        ApplySortToCollectionView();
        RefreshColumnHeaders();
        // NotifyVmAboutColumnClick(clickedSpec.Column);
    }

    private void ApplySortToCollectionView()
    {
        var view = CollectionViewSource.GetDefaultView(CardsGrid.ItemsSource);
        if (view is null)
            return;

        view.SortDescriptions.Clear();

        var activeSpec = columnSpecs.FirstOrDefault(spec => spec.Column == activeSortColumn);
        if (activeSpec is null)
            return;

        view.SortDescriptions.Add(new SortDescription(activeSpec.SortMemberPath, activeSortDirection));
    }

    private void CardsGrid_ColumnReordered(object? sender, DataGridColumnEventArgs e)
    {
        if (specByColumn.Count == 0)
            return;

        int order = 0;
        var visible = specByColumn
            .OrderBy(pair => pair.Key.DisplayIndex)
            .Select(pair => pair.Value)
            .ToList();

        foreach (var spec in visible)
            spec.Order = order++;

        foreach (var hidden in columnSpecs.Where(spec => !spec.IsVisible).OrderBy(spec => spec.Order))
            hidden.Order = order++;
    }

    private void CardsGrid_PreviewMouseRightButtonUp(object sender, MouseButtonEventArgs e)
    {
        if (FindAncestor<DataGridColumnHeader>(e.OriginalSource as DependencyObject) is not DataGridColumnHeader header)
            return;

        e.Handled = true;
        OpenColumnsMenu(header);
    }

    private void CardsGrid_PreviewMouseRightButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (FindAncestor<DataGridColumnHeader>(e.OriginalSource as DependencyObject) is not null)
            return;

        var row = FindAncestor<DataGridRow>(e.OriginalSource as DependencyObject);
        if (row?.Item is null)
            return;

        if (!row.IsSelected)
        {
            CardsGrid.SelectedItems.Clear();
            row.IsSelected = true;
        }
    }

    private void CardsContextMenu_Opened(object sender, RoutedEventArgs e)
    {
        if (DataContext is not BrowseVM vm)
            return;

        SyncSelectionWithViewModel();

        if (vm.GetSelectedCards().Count == 0 && sender is ContextMenu menu)
        {
            menu.IsOpen = false;
            return;
        }

        vm.OpenCtxMenu();
    }

    private void CardsGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        SyncSelectionWithViewModel();
    }

    private void SyncSelectionWithViewModel()
    {
        if (DataContext is not BrowseVM vm)
            return;

        var selected = CardsGrid.SelectedItems
            .OfType<object>()
            .ToHashSet();

        foreach (var card in vm.Cards)
            card.IsSelected = selected.Contains(card);
    }

    private void OpenColumnsMenu(DataGridColumnHeader header)
    {
        var menu = new ContextMenu
        {
            PlacementTarget = header,
            Placement = PlacementMode.Bottom
        };

        foreach (var spec in columnSpecs.OrderBy(spec => spec.Order))
        {
            var item = new MenuItem
            {
                Header = spec.Header,
                InputGestureText = spec.IsVisible ? "✔" : string.Empty
            };

            item.Click += (_, _) => ToggleColumnVisibility(spec);
            menu.Items.Add(item);
        }

        header.ContextMenu = menu;
        menu.IsOpen = true;
    }

    private void ToggleColumnVisibility(BrowseColumnSpec spec)
    {
        if (spec.IsVisible && columnSpecs.Count(column => column.IsVisible) == 1)
            return;

        spec.IsVisible = !spec.IsVisible;

        if (!columnSpecs.Any(column => column.IsVisible))
            return;

        if (!columnSpecs.Any(column => column.Column == activeSortColumn && column.IsVisible))
            activeSortColumn = columnSpecs.First(column => column.IsVisible).Column;

        RebuildColumns();
        ApplySortToCollectionView();
    }

    // private void NotifyVmAboutColumnClick(BrowseColumn clickedColumn)
    // {
    //     if (DataContext is not BrowseVM vm)
    //         return;

    //     vm.ActiveBrowseColumn = clickedColumn;
    //     vm.SortDir = activeSortDirection == ListSortDirection.Ascending
    //         ? SortingDirection.Ascending
    //         : SortingDirection.Descending;

    //     if (TryMapToCardsOrder(clickedColumn, out var order))
    //         vm.SortOrder = order;

    //     vm.OnColumnClicked(clickedColumn);
    // }

    private static bool TryMapToCardsOrder(BrowseColumn column, out CardsOrder order)
    {
        switch (column)
        {
            case BrowseColumn.Id:
                order = CardsOrder.Id;
                return true;
            case BrowseColumn.Due:
                order = CardsOrder.Due;
                return true;
            case BrowseColumn.DayInterval:
                order = CardsOrder.Interval;
                return true;
            case BrowseColumn.LastModified:
                order = CardsOrder.LastModified;
                return true;
            case BrowseColumn.State:
                order = CardsOrder.State;
                return true;
            case BrowseColumn.Created:
                order = CardsOrder.Created;
                return true;
            default:
                order = default;
                return false;
        }
    }

    private static T? FindAncestor<T>(DependencyObject? source) where T : DependencyObject
    {
        var current = source;

        while (current is not null)
        {
            if (current is T found)
                return found;

            current = VisualTreeHelper.GetParent(current);
        }

        return null;
    }

    private sealed class BrowseColumnSpec(
        BrowseColumn column,
        string header,
        string bindingPath,
        int order,
        double width,
        bool isVisible = false,
        string? stringFormat = null,
        IValueConverter? converter = null,
        string? sortMemberPath = null)
    {
        public BrowseColumn Column { get; } = column;
        public string Header { get; } = header;
        public string BindingPath { get; } = bindingPath;
        public string SortMemberPath { get; } = sortMemberPath ?? bindingPath;
        public int Order { get; set; } = order;
        public DataGridLength Width { get; set; } = new(width);
        public bool IsVisible { get; set; } = isVisible;
        public string? StringFormat { get; } = stringFormat;
        public IValueConverter? Converter { get; } = converter;
    }

}
