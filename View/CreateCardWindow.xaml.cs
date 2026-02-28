using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using FlashMemo.Helpers;
using FlashMemo.ViewModel.Windows;
using FlashMemo.ViewModel.Wrappers;

namespace FlashMemo.View
{
    public partial class CreateCardWindow : Window, IViewFor<CreateCardVM>
    {
        public CreateCardVM VM { get; set; } = null!;
        public CreateCardWindow()
        {
            InitializeComponent();

            Loaded += OnLoaded;
            Closing += OnWindowClosing;
        }

        public async void AddButtonClicked(object sender, RoutedEventArgs e)
        {
            if (IsEditorEmpty())
            {
                MessageBox.Show(
                    "Cannot create a card, since all fields in editor are empty",
                    "Empty editor",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);

                return;
            }

            if (VM.WipCard.Note is not StandardNoteVM sn)
                throw new NotSupportedException("Only Standard notes supported for now.");

            // Serialize editors
            sn.FrontContent = XamlSerializer
                .ToXaml(FrontBox.Document);

            sn.BackContent = XamlSerializer
                .ToXaml(BackBox.Document);

            await VM.AddCardCommand.ExecuteAsync(null);

            // clearing the editor
            FrontBox.Document = new();
            BackBox.Document = new();
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            if (VM.WipCard.Note is not StandardNoteVM sn)
                throw new NotSupportedException("Only Standard notes supported for now.");

            // Load Front
            FrontBox.Document = XamlSerializer
                .FromXaml(sn.FrontContent);

            // Load Back
            BackBox.Document = XamlSerializer
                .FromXaml(sn.BackContent);
        }

        private void OnWindowClosing(object? sender, CancelEventArgs e)
        {
            if (IsEditorEmpty()) return;

            var result = MessageBox.Show(
                "You have unsaved changes. Discard them?",
                "Unsaved changes",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (result == MessageBoxResult.No)
                e.Cancel = true;
        }

        public void HistoryButtonClicked(object sender, RoutedEventArgs e)
        {
            if (sender is not Button btn || btn.ContextMenu is null)
                return;

            btn.ContextMenu.PlacementTarget = btn;
            btn.ContextMenu.IsOpen = true;
        }

        private bool IsEditorEmpty()
        {
            var front = XamlSerializer
                .GetPlainText(FrontBox.Document);

            var back = XamlSerializer
                .GetPlainText(BackBox.Document);

            return string.IsNullOrWhiteSpace(front)
                && string.IsNullOrWhiteSpace(back);
        }
    }
}
