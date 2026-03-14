using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using FlashMemo.Helpers;
using FlashMemo.ViewModel;
using FlashMemo.ViewModel.Windows;
using FlashMemo.ViewModel.Wrappers;

namespace FlashMemo.View
{
    public partial class CreateCardWindow : Window, IViewFor<CreateCardVM>
    {
        private const double EditorFontSize = 24;
        private TagChipInputController? tagInputController;
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
            ApplyEditorDefaults();

            if (tagInputController is not null)
                await tagInputController.RefreshAsync(reloadSuggestions: true);
        }

        private async void OnLoaded(object sender, RoutedEventArgs e)
        {
            if (VM.WipCard.Note is null) throw new NullReferenceException();

            if (VM.WipCard.Note is not StandardNoteVM sn)
                throw new NotSupportedException("Only Standard notes supported for now.");

            // Load Front
            FrontBox.Document = XamlSerializer
                .FromXaml(sn.FrontContent);

            // Load Back
            BackBox.Document = XamlSerializer
                .FromXaml(sn.BackContent);

            ApplyEditorDefaults();

            if (VM is not ITagManagerHost host || host.TagManager is null)
                throw new NotSupportedException($"{nameof(CreateCardVM)} should expose a non-null {nameof(ITagManagerHost.TagManager)}.");

            tagInputController = new(
                host.TagManager,
                TagChipPanel,
                TagInputBox,
                TagSuggestionsPopup,
                TagSuggestionsList);

            await tagInputController.InitializeAsync();
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

        private void ApplyEditorDefaults()
        {
            FrontBox.FontSize = EditorFontSize;
            BackBox.FontSize = EditorFontSize;
        }
    }
}
