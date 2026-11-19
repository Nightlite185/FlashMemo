using System.Windows.Controls;
using System.Windows.Input;
using FlashMemo.ViewModel;

namespace FlashMemo.View;

public partial class TagInput : UserControl
{
    public bool IsCommitting { get; private set; }

    public TagInput() => InitializeComponent();

    private async void OnPreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key != Key.Enter)
            return;

        // Intercept before SfTextBoxExt's native Enter handling so one key commits once.
        e.Handled = true;
        await CommitAsync();
    }

    internal async Task CommitAsync()
    {
        if (IsCommitting || DataContext is not ICardTagsVM vm || string.IsNullOrWhiteSpace(Input.Text))
            return;

        string text = Input.Text;
        IsCommitting = true;
        Input.IsEnabled = false;

        try
        {
            // The VM publishes the selected token only after persistence succeeds.
            Input.Text = await vm.TryAddTagAsync(text) is not null ? string.Empty : text;
        }
        
        finally
        {
            Input.IsEnabled = true;
            IsCommitting = false;
            Input.Focus();
        }
    }

    public void Refresh()
    {
        if (DataContext is ICardTagsVM vm)
            vm.ReloadSelection();

        Input.Text = string.Empty;
    }
}