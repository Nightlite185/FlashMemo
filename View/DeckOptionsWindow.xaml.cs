using FlashMemo.ViewModel.Windows;
using FlashMemo.ViewModel.Wrappers;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;

namespace FlashMemo.View;
    
public partial class DeckOptionsWindow : Window, IViewFor<DeckOptionsMenuVM>
{
    public DeckOptionsMenuVM VM { get; set; } = null!;
    public DeckOptionsWindow()
    {
        InitializeComponent();
    }

    private async void TryDeletePreset(object sender, RoutedEventArgs e)
    {
        var result = MessageBoxResult.Yes;

        if (VM.RemoveRequiresConfirmation)
        {
            result = MessageBox.Show(
            "Are you sure you want to delete currently viewed preset? Every deck currently referencing this preset, will now be assigned to the default one. Do you wish to proceed?",
            "Are you sure?",
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning);
        }

        if (result == MessageBoxResult.Yes)
            await VM.RemovePresetCommand.ExecuteAsync(null);
    }

    private async void Window_Closing(object sender, CancelEventArgs e)
    {
        var answer = GetApproval();

        if (answer == MessageBoxResult.Cancel)
            e.Cancel = true;

        else if (answer == MessageBoxResult.Yes)
            await VM.SaveChangesCommand.ExecuteAsync(null);
    }

    private MessageBoxResult GetApproval()
    {
        if (VM.IsCurrentModified)
        {
            return MessageBox.Show(
                "You have unsaved changes. Do you want to save them?",
                "Unsaved changes",
                MessageBoxButton.YesNoCancel,
                MessageBoxImage.Warning
            );
        }

        return MessageBoxResult.No; // no means discard, bc nothing changed anyway.
    }

    private async void PresetSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (e.AddedItems.Count == 0)
            return;

        var combo = (ComboBox)sender;
        var neww = e.AddedItems[0] as DeckOptionsVM;

        var old = e.RemovedItems.Count == 0 
            ? null 
            : e.RemovedItems[0] as DeckOptionsVM;

        if (old == neww
        || neww == VM.CurrentOptions) 
            return;
        
        if (old is not null)
        {
            var answer = GetApproval();

            if (answer == MessageBoxResult.Cancel)
            {
                // Restore UI only (VM untouched)
                combo.SelectedItem = old;
                return;
            }

            if (answer == MessageBoxResult.Yes)
                await VM.SaveChangesCommand.ExecuteAsync(null);
        }

        // NOW commit to VM
        VM.ChangePresetCommand.Execute(neww);
    }

}
