using System.Windows;
using FlashMemo.Helpers;
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
            if (VM.Card.Note is not StandardNoteVM sn)
                throw new NotSupportedException("Only Standard notes supported for now.");

            FrontBox.Document = XamlSerializer
                .FromXaml(sn.FrontContent);

            BackBox.Document = XamlSerializer
                .FromXaml(sn.BackContent);

            ApplyEditorDefaults();
        }

        private void SaveEditorsToVm()
        {
            if (VM.Card.Note is not StandardNoteVM sn)
                throw new NotSupportedException("Only Standard notes supported for now.");

            sn.FrontContent = XamlSerializer
                .ToXaml(FrontBox.Document);

            sn.BackContent = XamlSerializer
                .ToXaml(BackBox.Document);
        }

        private void ApplyEditorDefaults()
        {
            FrontBox.FontSize = EditorFontSize;
            BackBox.FontSize = EditorFontSize;
            FrontBox.Document.FontSize = EditorFontSize;
            BackBox.Document.FontSize = EditorFontSize;
        }
    }
}
