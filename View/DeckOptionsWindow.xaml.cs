using FlashMemo.ViewModel.Windows;
using FlashMemo.ViewModel.Wrappers;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace FlashMemo.View;
    
public partial class DeckOptionsWindow : Window, IViewFor<DeckOptionsMenuVM>
{
    public DeckOptionsMenuVM VM { get; set; } = null!;
    public DeckOptionsWindow()
    {
        InitializeComponent();
    }

    private async void PresetSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (e.AddedItems.Count == 0 || e.AddedItems[0] is not DeckOptionsVM selected)
            return;

        var combo = (ComboBox)sender;

        if (selected == VM.CurrentOptions)
            return;

        if (VM.ChangePresetCommand.CanExecute(selected))
            await VM.ChangePresetCommand.ExecuteAsync(selected);

        // A cancelled discard prompt leaves the previous preset selected.
        combo.SetCurrentValue(ComboBox.SelectedItemProperty, VM.CurrentOptions);
    }

    private async void CreatePresetBox_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Escape)
        {
            VM.CancelCreatePresetCommand.Execute(null);
            e.Handled = true;
            return;
        }

        if (e.Key != Key.Enter)
            return;

        if (string.IsNullOrWhiteSpace(VM.NewPresetName))
        {
            VM.CancelCreatePresetCommand.Execute(null);
            e.Handled = true;
            return;
        }

        await VM.AddNewPresetCommand.ExecuteAsync(null);
        e.Handled = true;
    }

    private void CreatePresetBox_LostFocus(object sender, RoutedEventArgs e)
    {
        if (VM.AddNewPresetCommand.IsRunning)
            return;

        Keyboard.ClearFocus();
        VM.CancelCreatePresetCommand.Execute(null);
    }

    private async void RenamePresetBox_KeyDown(object sender, KeyEventArgs e)
    {
        var preset = VM.CurrentOptions;

        if (e.Key == Key.Escape)
        {
            preset.CancelRenameCommand.Execute(null);
            e.Handled = true;
            return;
        }

        if (e.Key != Key.Enter)
            return;

        if (string.IsNullOrWhiteSpace(preset.TempName) ||
            preset.TempName == preset.Name)
        {
            preset.CancelRenameCommand.Execute(null);
            e.Handled = true;
            return;
        }

        await VM.RenamePresetCommand.ExecuteAsync(preset);
        e.Handled = true;
    }

    private void RenamePresetBox_LostFocus(object sender, RoutedEventArgs e)
    {
        Keyboard.ClearFocus();
        VM.CurrentOptions.CancelRenameCommand.Execute(null);
    }

    private void PresetNameBox_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
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
}