using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using FlashMemo.Helpers;
using FlashMemo.ViewModel.Windows;
using FlashMemo.ViewModel.Wrappers;

namespace FlashMemo.View
{
    public partial class CreateCardWindow : Window, IViewFor<CreateCardVM>
    {
        public bool SavingLocked { get; private set; }
        private const double EditorFontSize = 24;
        public CreateCardVM VM { get; set; } = null!;
        public CreateCardWindow()
        {
            InitializeComponent();

            Loaded += OnLoaded;
            Closing += OnWindowClosing;
            PreviewKeyDown += OnWindowPreviewKeyDown;
        }

        public async void AddButtonClicked(object sender, RoutedEventArgs e)
        {
            if (SavingLocked || TagsInput.IsCommitting)
                return;
                
            SavingLocked = true;
            FrontBox.IsReadOnly = true;
            BackBox.IsReadOnly = true;

            try
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

                var sn = TryGetNote(VM.WipCard.Note);

                // Serialize editors
                sn.FrontContent = XamlSerializer
                    .ToXaml(FrontBox.Document);

                sn.BackContent = XamlSerializer
                    .ToXaml(BackBox.Document);

                if (!await VM.AddCard())
                    return;

                ResetEditor();
            }

            finally
            {
                SavingLocked = false;
                FrontBox.IsReadOnly = false;
                BackBox.IsReadOnly = false;
            }
        }

        private void ResetEditor()
        {
            FrontBox.Document = new();
            BackBox.Document = new();
            ApplyEditorDefaults();

            FrontBox.Focus();
            Keyboard.Focus(FrontBox);

            TagsInput.Refresh();
        }

        private static StandardNoteVM TryGetNote(NoteVM note)
        {
            return note as StandardNoteVM
                ?? throw new NotSupportedException(
                "Only Standard notes supported for now.");
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            if (VM.WipCard.Note is null)
                throw new NullReferenceException();

            var sn = TryGetNote(VM.WipCard.Note);

            // Load Front
            FrontBox.Document = XamlSerializer
                .FromXaml(sn.FrontContent);

            // Load Back
            BackBox.Document = XamlSerializer
                .FromXaml(sn.BackContent);

            ApplyEditorDefaults();
        }

        private void OnWindowClosing(object? sender, CancelEventArgs e)
        {
            //* ignore all unsaved changes on fatal shutdown started.
            if (App.IsFatalShutdownStarted || IsEditorEmpty())
                return;

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
        private void OnWindowPreviewKeyDown(object sender, KeyEventArgs e)
        {
            bool ctrlPressed = Keyboard.Modifiers.HasFlag(ModifierKeys.Control);
            bool enterClicked = e.Key is Key.Enter or Key.Return;

            if (ctrlPressed && enterClicked)
            {
                AddButtonClicked(this, new RoutedEventArgs());
                e.Handled = true;
            }
        }
    }
}