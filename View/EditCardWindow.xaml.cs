using System.ComponentModel;
using System.Windows;
using FlashMemo.Helpers;
using FlashMemo.Model.Persistence;
using FlashMemo.ViewModel.Windows;
using FlashMemo.ViewModel.Wrappers;

namespace FlashMemo.View
{
    public partial class EditCardWindow : Window, IViewFor<CardEditorVM>
    {
        private const double EditorFontSize = 24;

        public CardEditorVM VM { get; set; } = null!;
        public EditCardWindow()
        {
            InitializeComponent();
            
            Loaded += OnLoaded;
            Closing += OnWindowClosing;
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
            => LoadEditorsFromVm();

        public async void SaveButtonClicked(object sender, RoutedEventArgs e)
        {
            SaveEditorsToVm();
            await VM.SaveChangesCommand.ExecuteAsync(null);
        }

        public async void RevertButtonClicked(object sender, RoutedEventArgs e)
        {
            await VM.RevertChangesCommand.ExecuteAsync(null);
            LoadEditorsFromVm();
        }

        private void LoadEditorsFromVm()
        {
            switch (VM.Card.Note.Type)
            {
                case NoteTypes.Standard:
                    if (VM.Card.Note is not StandardNoteVM sn)
                        throw new NotSupportedException(
                            $"Note type '{VM.Card.Note.Type}' is not mapped to {nameof(StandardNoteVM)}.");

                    FrontBox.Document = XamlSerializer
                        .FromXaml(sn.FrontContent);

                    BackBox.Document = XamlSerializer
                        .FromXaml(sn.BackContent);
                    break;

                default:
                    throw new NotSupportedException(
                        $"Note type '{VM.Card.Note.Type}' is not supported yet.");
            }

            ApplyEditorDefaults();
        }

        private void SaveEditorsToVm()
        {
            switch (VM.Card.Note.Type)
            {
                case NoteTypes.Standard:
                    if (VM.Card.Note is not StandardNoteVM sn)
                        throw new NotSupportedException(
                            $"Note type '{VM.Card.Note.Type}' is not mapped to {nameof(StandardNoteVM)}.");

                    sn.FrontContent = XamlSerializer
                        .ToXaml(FrontBox.Document);

                    sn.BackContent = XamlSerializer
                        .ToXaml(BackBox.Document);
                    break;

                default:
                    throw new NotSupportedException(
                        $"Note type '{VM.Card.Note.Type}' is not supported yet.");
            }
        }

        private void OnWindowClosing(object? sender, CancelEventArgs e)
        {
            if (IsSameAsSaved())
                return;

            var result = MessageBox.Show(
                "You have unsaved changes. Discard them?",
                "Unsaved changes",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (result == MessageBoxResult.No)
                e.Cancel = true;
        }

        private void ApplyEditorDefaults()
        {
            FrontBox.FontSize = EditorFontSize;
            BackBox.FontSize = EditorFontSize;
        }

        private bool IsSameAsSaved()
            => VM.Card.IsSameAsSavedNote(
                XamlSerializer.ToXaml(FrontBox.Document),
                XamlSerializer.ToXaml(BackBox.Document));
    }
}
