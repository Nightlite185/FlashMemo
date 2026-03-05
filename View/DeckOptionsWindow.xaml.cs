using FlashMemo.ViewModel.Windows;
using FlashMemo.ViewModel.Wrappers;
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

    private async void PresetSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (e.AddedItems.Count == 0)
            return;

        var combo = (ComboBox)sender;
        var neww = e.AddedItems[0] as DeckOptionsVM;

        var old = e.RemovedItems.Count == 0
            ? null 
            : e.RemovedItems[0] as DeckOptionsVM;

        if (old == neww || neww == VM.CurrentOptions) 
            return;
        
        if (old is not null && !await VM.CanDiscardAsync())
        {
            // Restore UI only (VM untouched)
            combo.SelectedItem = old;
            return;
        }

        // NOW commit to VM
        VM.ChangePresetCommand.Execute(neww);
    }
}
