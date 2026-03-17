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
    ICardTagsVM vm,
    WrapPanel chipHostPanel,
    TextBox inputBox,
    Popup suggestionsPopup,
    ListBox suggestionsList)
{
    private static readonly ControlTemplate FlatButtonTemplate = CreateFlatButtonTemplate();

    private List<TagVM> allTags = [];
    private readonly List<TagVM> displayTags = [];
    private TagVM? openedTag;
    private int inputSlotIndex;

    public async Task InitializeAsync()
    {
        inputBox.TextChanged += OnInputTextChanged;
        inputBox.PreviewKeyDown += OnInputPreviewKeyDown;
        inputBox.LostKeyboardFocus += OnInputLostKeyboardFocus;

        suggestionsList.PreviewMouseLeftButtonUp += OnSuggestionMouseUp;

        await ReloadAllTagsAsync();
        ResetDisplayFromVm();
        RebuildChips();
    }

    public async Task RefreshAsync(bool reloadSuggestions = false)
    {
        if (reloadSuggestions)
            await ReloadAllTagsAsync();

        ResetDisplayFromVm();
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
                e.Handled = NavigateAcrossChips(-1);
                break;

            case Key.Right when inputBox.CaretIndex == inputBox.Text.Length:
                e.Handled = NavigateAcrossChips(1);
                break;

            case Key.Back when inputBox.Text.Length == 0 && inputBox.CaretIndex == 0:
                e.Handled = NavigateAcrossChips(-1, removeCurrentOpenTagIfInputEmpty: true);
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
        allTags = [.. vm.AllTags
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
        var normalized = tagName.Trim();

        if (openedTag is not null)
        {
            await CommitOpenedChipAsync(normalized);
            inputBox.Focus();
            HideSuggestions();
            return;
        }

        if (string.IsNullOrWhiteSpace(normalized))
            return;

        var added = await vm.AddTagAsync(normalized);

        if (added is null || displayTags.Any(t => t.Id == added.Id))
            return;

        displayTags.Insert(inputSlotIndex, added);
        inputSlotIndex = Math.Min(displayTags.Count, inputSlotIndex + 1);

        inputBox.Clear();
        await ReloadAllTagsAsync();
        RebuildChips();
        RefreshSuggestions();

        inputBox.Focus();
        HideSuggestions();
    }

    private async Task CommitOpenedChipAsync(string normalized)
    {
        var previous = openedTag!;

        if (string.IsNullOrWhiteSpace(normalized) ||
            string.Equals(previous.Name, normalized, StringComparison.OrdinalIgnoreCase))
        {
            previous.Name = string.IsNullOrWhiteSpace(normalized)
                ? previous.Name
                : normalized;

            displayTags.Insert(inputSlotIndex, previous);
            openedTag = null;

            inputBox.Clear();
            inputSlotIndex = Math.Min(displayTags.Count, inputSlotIndex + 1);
            RebuildChips();
            RefreshSuggestions();
            return;
        }

        vm.RemoveTag(previous.Id);

        openedTag = null;
        inputBox.Clear();

        var added = await vm.AddTagAsync(normalized);

        if (added is not null && displayTags.All(t => t.Id != added.Id))
            displayTags.Insert(inputSlotIndex, added);

        inputSlotIndex = Math.Min(displayTags.Count, inputSlotIndex + 1);
        await ReloadAllTagsAsync();
        RebuildChips();
        RefreshSuggestions();
    }

    private bool NavigateAcrossChips(int direction, bool removeCurrentOpenTagIfInputEmpty = false)
    {
        if (direction > 0 && inputSlotIndex >= displayTags.Count)
            return false;

        var hadOpen = false;

        if (removeCurrentOpenTagIfInputEmpty && openedTag is not null && inputBox.Text.Length == 0)
        {
            vm.RemoveTag(openedTag.Id);
            openedTag = null;
            inputBox.Clear();
            hadOpen = true;
        }
        else
        {
            hadOpen = CloseOpenedChipForTraversal();
        }

        int targetIndex;

        if (direction < 0)
        {
            targetIndex = inputSlotIndex - 1;
        }
        else
        {
            targetIndex = hadOpen ? inputSlotIndex + 1 : inputSlotIndex;
        }

        if (targetIndex < 0 || targetIndex >= displayTags.Count)
        {
            RebuildChips();
            inputBox.Focus();
            return false;
        }

        var target = displayTags[targetIndex];
        displayTags.RemoveAt(targetIndex);

        openedTag = target;
        inputSlotIndex = targetIndex;

        inputBox.Text = target.Name;
        inputBox.CaretIndex = direction < 0 ? inputBox.Text.Length : 0;

        RebuildChips();
        inputBox.Focus();
        RefreshSuggestions();
        return true;
    }

    private bool CloseOpenedChipForTraversal()
    {
        if (openedTag is null)
            return false;

        displayTags.Insert(inputSlotIndex, openedTag);
        openedTag = null;
        inputBox.Clear();
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
        inputSlotIndex = Math.Clamp(inputSlotIndex, 0, displayTags.Count);
        chipHostPanel.Children.Clear();

        for (var i = 0; i < displayTags.Count; i++)
        {
            if (i == inputSlotIndex)
                chipHostPanel.Children.Add(inputBox);

            chipHostPanel.Children.Add(CreateChip(displayTags[i]));
        }

        if (inputSlotIndex >= displayTags.Count)
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

        var label = new TextBlock
        {
            Text = tag.Name,
            Foreground = Brushes.White,
            FontSize = 22,
            VerticalAlignment = VerticalAlignment.Center
        };

        var editButton = new Button
        {
            Template = FlatButtonTemplate,
            Background = Brushes.Transparent,
            BorderThickness = new Thickness(0),
            Padding = new Thickness(0),
            Margin = new Thickness(0, 0, 6, 0),
            Cursor = Cursors.Hand,
            Focusable = false,
            Content = label
        };
        editButton.MouseEnter += (_, _) => label.TextDecorations = TextDecorations.Underline;
        editButton.MouseLeave += (_, _) => label.TextDecorations = null;
        editButton.Click += (_, _) => OpenChipByReference(tag, caretAtEnd: true);

        var removeButton = new Button
        {
            Template = FlatButtonTemplate,
            Background = Brushes.Transparent,
            BorderThickness = new Thickness(0),
            Width = 14,
            Height = 14,
            Padding = new Thickness(0),
            Cursor = Cursors.Hand,
            Focusable = false
        };
        removeButton.Click += async (_, _) => await RemoveChipAsync(tag);

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

    private void OpenChipByReference(TagVM tag, bool caretAtEnd)
    {
        CloseOpenedChipForTraversal();

        var index = displayTags.IndexOf(tag);

        if (index < 0)
            return;

        displayTags.RemoveAt(index);
        openedTag = tag;
        inputSlotIndex = index;

        inputBox.Text = tag.Name;
        inputBox.CaretIndex = caretAtEnd ? inputBox.Text.Length : 0;

        RebuildChips();
        inputBox.Focus();
        RefreshSuggestions();
    }

    private async Task RemoveChipAsync(TagVM tag)
    {
        var index = displayTags.IndexOf(tag);

        if (index < 0)
            return;

        if (!vm.RemoveTag(tag.Id))
            return;

        displayTags.RemoveAt(index);

        if (index < inputSlotIndex)
            inputSlotIndex--;

        await ReloadAllTagsAsync();
        RebuildChips();
        RefreshSuggestions();
        inputBox.Focus();
    }

    private void ResetDisplayFromVm()
    {
        displayTags.Clear();
        displayTags.AddRange(vm.CardTags);

        openedTag = null;
        inputSlotIndex = displayTags.Count;
        inputBox.Clear();
    }

    private static ControlTemplate CreateFlatButtonTemplate()
    {
        var border = new FrameworkElementFactory(typeof(Border));
        border.SetValue(Border.BackgroundProperty, Brushes.Transparent);

        var presenter = new FrameworkElementFactory(typeof(ContentPresenter));
        presenter.SetValue(FrameworkElement.HorizontalAlignmentProperty, HorizontalAlignment.Center);
        presenter.SetValue(FrameworkElement.VerticalAlignmentProperty, VerticalAlignment.Center);

        border.AppendChild(presenter);

        return new ControlTemplate(typeof(Button))
        {
            VisualTree = border
        };
    }
}
