using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using FlashMemo.ViewModel.Popups;
using Microsoft.Win32;

namespace FlashMemo.View;

public partial class ExportUC : UserControl
{
    public ExportUC()
    {
        InitializeComponent();
    }

    private void BrowseButton_Click(object sender, RoutedEventArgs e)
    {
        if (DataContext is not ExportVM vm)
            return;

        var (filter, extension) = vm.Format switch
        {
            ExportFormat.txt => ("Text files (*.txt)|*.txt|All files (*.*)|*.*", ".txt"),
            _ => ("All files (*.*)|*.*", string.Empty)
        };

        var dialog = new SaveFileDialog
        {
            AddExtension = extension.Length > 0,
            CheckPathExists = true,
            DefaultExt = extension,
            Filter = filter,
            OverwritePrompt = true,
            Title = "Export cards"
        };

        if (Directory.Exists(vm.ExportPath))
        {
            dialog.InitialDirectory = vm.ExportPath;
            dialog.FileName = $"FlashMemo export{extension}";
        }
        else if (!string.IsNullOrWhiteSpace(vm.ExportPath))
        {
            string? directory = Path.GetDirectoryName(vm.ExportPath);
            if (!string.IsNullOrWhiteSpace(directory) && Directory.Exists(directory))
                dialog.InitialDirectory = directory;

            dialog.FileName = Path.GetFileName(vm.ExportPath);
        }

        if (dialog.ShowDialog() != true)
            return;

        vm.ExportPath = dialog.FileName;
        ExportPathTextBox.CaretIndex = ExportPathTextBox.Text.Length;
        ExportPathTextBox.Focus();
    }

    private async void Root_PreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (DataContext is not ExportVM vm)
            return;

        if (e.Key == Key.Escape)
        {
            vm.CloseCommand.Execute(null);
            e.Handled = true;
            return;
        }

        if (e.Key != Key.Enter || Keyboard.Modifiers != ModifierKeys.Control)
            return;

        if (vm.ConfirmCommand.CanExecute(null))
            await vm.ConfirmCommand.ExecuteAsync(null);

        e.Handled = true;
    }
}
