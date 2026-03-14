using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows.Threading;
using FlashMemo.ViewModel;
using FlashMemo.ViewModel.Wrappers;

namespace FlashMemo.Helpers;

internal sealed class TagChipInputController(
    ITagManagerVM vm,
    WrapPanel chipHostPanel,
    TextBox inputBox,
    Popup suggestionsPopup,
    ListBox suggestionsList)
{
    private List<TagVM> allTags = [];

    public async Task InitializeAsync()
    {
        inputBox.TextChanged += OnInputTextChanged;
        inputBox.PreviewKeyDown += OnInputPreviewKeyDown;
        inputBox.LostKeyboardFocus += OnInputLostKeyboardFocus;

        suggestionsList.PreviewMouseLeftButtonUp += OnSuggestionMouseUp;

        await ReloadAllTagsAsync();
        RebuildChips();
    }

    public async Task RefreshAsync(bool reloadSuggestions = false)
    {
        if (reloadSuggestions)
            await ReloadAllTagsAsync();

        RebuildChips();
        RefreshSuggestions();
    }

    private async void OnInputPreviewKeyDown(object sender, KeyEventArgs e)
    {
        switch (e.Key)
        {
            case Key.Enter:
                e.Handled = true;
                await CommitFromInputOrSuggestionAsync();
                break;

            case Key.Tab when suggestionsPopup.IsOpen && suggestionsList.Items.Count > 0:
                e.Handled = true;
                CycleSuggestions(Keyboard.Modifiers.HasFlag(ModifierKeys.Shift) ? -1 : 1);
                break;

            case Key.Down when suggestionsPopup.IsOpen && suggestionsList.Items.Count > 0:
                e.Handled = true;
                CycleSuggestions(1);
                break;

            case Key.Up when suggestionsPopup.IsOpen && suggestionsList.Items.Count > 0:
                e.Handled = true;
                CycleSuggestions(-1);
                break;

            case Key.Escape:
                e.Handled = true;
                HideSuggestions();
                break;

            case Key.Left when inputBox.CaretIndex == 0:
                e.Handled = TryEditBoundaryTag(fromEnd: true);
                break;

            case Key.Right when inputBox.Text.Length == 0 && inputBox.CaretIndex == 0:
                e.Handled = TryEditBoundaryTag(fromEnd: false);
                break;
        }
    }

    private async void OnSuggestionMouseUp(object sender, MouseButtonEventArgs e)
    {
        if (suggestionsList.SelectedItem is not TagVM selected)
            return;

        await CommitTagAsync(selected.Name);
    }

    private void OnInputLostKeyboardFocus(object sender, KeyboardFocusChangedEventArgs e)
    {
        inputBox.Dispatcher.BeginInvoke(() =>
        {
            if (inputBox.IsKeyboardFocusWithin || suggestionsList.IsKeyboardFocusWithin)
                return;

            HideSuggestions();
        }, DispatcherPriority.Background);
    }

    private void OnInputTextChanged(object sender, TextChangedEventArgs e)
        => RefreshSuggestions();

    private async Task ReloadAllTagsAsync()
    {
        allTags = [.. (await vm.GetAllExistingTagsAsync())
            .OrderBy(t => t.Name, StringComparer.OrdinalIgnoreCase)];
    }

    private async Task CommitFromInputOrSuggestionAsync()
    {
        var tagText = suggestionsList.SelectedItem is TagVM selected
            ? selected.Name
            : inputBox.Text;

        await CommitTagAsync(tagText);
    }

    private async Task CommitTagAsync(string tagName)
    {
        var added = await vm.AddTagAsync(tagName);

        if (added is null)
            return;

        inputBox.Clear();
        await RefreshAsync(reloadSuggestions: true);

        inputBox.Focus();
        HideSuggestions();
    }

    private bool TryEditBoundaryTag(bool fromEnd)
    {
        var tags = vm.CardTags.ToList();

        if (tags.Count == 0)
            return false;

        var tag = fromEnd ? tags[^1] : tags[0];

        if (!vm.RemoveTag(tag.Id))
            return false;

        RebuildChips();

        inputBox.Text = tag.Name;
        inputBox.CaretIndex = fromEnd ? inputBox.Text.Length : 0;

        inputBox.Focus();
        RefreshSuggestions();
        return true;
    }

    private void CycleSuggestions(int direction)
    {
        var count = suggestionsList.Items.Count;

        if (count == 0)
            return;

        var index = suggestionsList.SelectedIndex;

        if (index < 0)
            index = direction > 0 ? 0 : count - 1;
        else
            index = (index + direction + count) % count;

        suggestionsList.SelectedIndex = index;
        suggestionsList.ScrollIntoView(suggestionsList.SelectedItem);
    }

    private void RefreshSuggestions()
    {
        var query = inputBox.Text.Trim();

        if (string.IsNullOrWhiteSpace(query))
        {
            HideSuggestions();
            return;
        }

        var selectedNames = vm.CardTags
            .Select(t => t.Name.Trim())
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var matches = allTags
            .Where(t => !selectedNames.Contains(t.Name.Trim()))
            .Where(t => t.Name.Contains(query, StringComparison.OrdinalIgnoreCase))
            .OrderByDescending(t => t.Name.StartsWith(query, StringComparison.OrdinalIgnoreCase))
            .ThenBy(t => t.Name, StringComparer.OrdinalIgnoreCase)
            .ToList();

        suggestionsList.ItemsSource = matches;

        if (matches.Count == 0)
        {
            HideSuggestions();
            return;
        }

        if (suggestionsList.SelectedIndex >= matches.Count)
            suggestionsList.SelectedIndex = -1;

        suggestionsPopup.IsOpen = true;
    }

    private void HideSuggestions()
    {
        suggestionsPopup.IsOpen = false;
        suggestionsList.ItemsSource = null;
        suggestionsList.SelectedIndex = -1;
    }

    private void RebuildChips()
    {
        chipHostPanel.Children.Clear();

        foreach (var tag in vm.CardTags)
            chipHostPanel.Children.Add(CreateChip(tag));

        chipHostPanel.Children.Add(inputBox);
    }

    private UIElement CreateChip(TagVM tag)
    {
        var chipBorder = new Border
        {
            Background = new SolidColorBrush(Color.FromArgb(0x55, 0x00, 0x00, 0x00)),
            BorderBrush = new SolidColorBrush(Color.FromArgb(0x88, 0xFF, 0xFF, 0xFF)),
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(7),
            Margin = new Thickness(0, 0, 6, 4),
            Padding = new Thickness(8, 2, 4, 2),
            Tag = tag
        };

        var row = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            VerticalAlignment = VerticalAlignment.Center
        };

        var editButton = new Button
        {
            Background = Brushes.Transparent,
            BorderThickness = new Thickness(0),
            Padding = new Thickness(0),
            Margin = new Thickness(0, 0, 6, 0),
            Cursor = Cursors.Hand,
            Focusable = false,
            Tag = tag,
            Content = new TextBlock
            {
                Text = tag.Name,
                Foreground = Brushes.White,
                FontSize = 22,
                VerticalAlignment = VerticalAlignment.Center
            }
        };
        editButton.Click += OnChipEditClicked;

        var removeButton = new Button
        {
            Background = Brushes.Transparent,
            BorderThickness = new Thickness(0),
            Width = 14,
            Height = 14,
            Padding = new Thickness(0),
            Cursor = Cursors.Hand,
            Focusable = false,
            Tag = tag
        };
        removeButton.Click += OnChipRemoveClicked;

        removeButton.Content = new Path
        {
            Data = Geometry.Parse("M1,1 L9,9 M9,1 L1,9"),
            Stroke = Brushes.White,
            StrokeThickness = 1.6,
            StrokeStartLineCap = PenLineCap.Round,
            StrokeEndLineCap = PenLineCap.Round,
            Stretch = Stretch.None,
            Width = 10,
            Height = 10,
            SnapsToDevicePixels = true
        };

        row.Children.Add(editButton);
        row.Children.Add(removeButton);

        chipBorder.Child = row;
        return chipBorder;
    }

    private async void OnChipEditClicked(object sender, RoutedEventArgs e)
    {
        if (sender is not FrameworkElement { Tag: TagVM tag })
            return;

        if (!vm.RemoveTag(tag.Id))
            return;

        RebuildChips();

        inputBox.Text = tag.Name;
        inputBox.CaretIndex = inputBox.Text.Length;

        inputBox.Focus();
        await RefreshAsync();
    }

    private async void OnChipRemoveClicked(object sender, RoutedEventArgs e)
    {
        if (sender is not FrameworkElement { Tag: TagVM tag })
            return;

        if (!vm.RemoveTag(tag.Id))
            return;

        await RefreshAsync();
        inputBox.Focus();
    }
}
