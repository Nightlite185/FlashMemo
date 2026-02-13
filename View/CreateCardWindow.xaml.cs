using System.ComponentModel;
using System.Windows;
using System.Windows.Documents;
using FlashMemo.Helpers;
using FlashMemo.ViewModel.Windows;

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

            // Serialize editors
            VM.WipCard.FrontContentXAML = XamlSerializer
                .ToXaml(FrontBox.Document);

            VM.WipCard.BackContentXAML = XamlSerializer
                .ToXaml(BackBox.Document);

            await VM.AddCardCommand.ExecuteAsync(null);

            // clearing the editor
            FrontBox.Document = new(); //TODO: Clean this properly, this won't work.
            BackBox.Document = new();
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            // Load Front
            FrontBox.Document = XamlSerializer
                .FromXaml(VM.WipCard.FrontContentXAML);

            // Load Back
            BackBox.Document = XamlSerializer
                .FromXaml(VM.WipCard.BackContentXAML);
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
            // show ctx menu here listing the last ~8 cards created
        }

        private bool IsEditorEmpty()
        {
            var front = XamlSerializer
                .GetPlainText(FrontBox.Document);

            var back = XamlSerializer
                .GetPlainText(BackBox.Document);

            return string.IsNullOrWhiteSpace(front)
                && string.IsNullOrWhiteSpace(back);

            //TODO: CHECK FOR THE IMAGES SOMEHOW (won't appear from GetPlainText() probably)
        }
    }
}
