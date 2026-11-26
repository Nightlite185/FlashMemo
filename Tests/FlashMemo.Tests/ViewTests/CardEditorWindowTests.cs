using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Threading;
using FlashMemo.Model.Persistence;
using FlashMemo.Services;
using FlashMemo.View;
using FlashMemo.ViewModel;
using FlashMemo.ViewModel.Windows;
using FlashMemo.ViewModel.Wrappers;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Syncfusion.Licensing;

namespace FlashMemo.Tests.ViewTests;

[Collection("WPF")]
public class CardEditorWindowTests(WpfTestHost host)
{
    [Theory]
    [InlineData("<Paragraph><Run>front</Run></Paragraph>")]
    [InlineData("<Paragraph><Bold>front</Bold><Italic> text</Italic></Paragraph>")]
    [InlineData("<Paragraph TextAlignment=\"Center\" Margin=\"5\"><Run Foreground=\"Red\" FontSize=\"30\">front</Run></Paragraph>")]
    [InlineData("<List><ListItem><Paragraph>front</Paragraph></ListItem></List>")]
    [InlineData("")]
    public Task UntouchedEditor_ClosesWithoutWarningAfterWpfRewritesTheMarkup(string blocks) => host.Run(() =>
    {
        using var flow = CreateFlow(blocks);

        flow.Window.Close();

        flow.Closed.Should().BeTrue();
        flow.Dialogs.Verify(dialog => dialog.ShowWarning(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<DialogButtons>()), Times.Never);
    });

    [Theory]
    [InlineData("bold")]
    [InlineData("italic")]
    [InlineData("colour")]
    [InlineData("size")]
    [InlineData("alignment")]
    [InlineData("spacing")]
    [InlineData("back")]
    public Task FormattingOnlyChanges_AreProtectedAndRevertRestoresTheSavedDocument(string change) => host.Run(() =>
    {
        using var flow = CreateFlow();
        var document = change == "back" ? flow.Back.Document : flow.Front.Document;
        var range = new TextRange(document.ContentStart, document.ContentEnd);
        string text = range.Text;
        switch (change)
        {
            case "bold":
            case "back": range.ApplyPropertyValue(TextElement.FontWeightProperty, FontWeights.Bold); break;
            case "italic": range.ApplyPropertyValue(TextElement.FontStyleProperty, FontStyles.Italic); break;
            case "colour": range.ApplyPropertyValue(TextElement.ForegroundProperty, Brushes.Red); break;
            case "size": range.ApplyPropertyValue(TextElement.FontSizeProperty, 32.0); break;
            case "alignment": ((Paragraph)document.Blocks.FirstBlock).TextAlignment = TextAlignment.Center; break;
            case "spacing": ((Paragraph)document.Blocks.FirstBlock).Margin = new Thickness(12); break;
        }
        range.Text.Should().Be(text);

        flow.Window.Close();
        flow.Closed.Should().BeFalse();
        flow.Dialogs.Verify(dialog => dialog.ShowWarning(It.IsAny<string>(), "Unsaved changes", DialogButtons.YesNo), Times.Once);

        flow.Window.RevertButtonClicked(flow.Window, new RoutedEventArgs());
        flow.Window.Close();

        flow.Closed.Should().BeTrue();
        flow.Dialogs.Verify(dialog => dialog.ShowWarning(It.IsAny<string>(), "Unsaved changes", DialogButtons.YesNo), Times.Once);
    });

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public Task Save_LocksTheWholeDraftRejectsRepeatedHandlersAndClosesOnlyAfterSuccess(bool succeeds) => host.Run(() =>
    {
        using var flow = CreateFlow();
        var completion = new TaskCompletionSource();
        flow.Service.Setup(service => service.SaveEditedCard(It.IsAny<CardEntity>(), CardAction.Modify, null))
            .Returns(completion.Task);
        new TextRange(flow.Front.Document.ContentStart, flow.Front.Document.ContentEnd)
            .ApplyPropertyValue(TextElement.FontWeightProperty, FontWeights.Bold);
        flow.Card.Tags.Clear();
        flow.Card.ChangeDeck(new Deck { Id = 9, UserId = 7, Name = "New deck" });

        flow.Window.SaveButtonClicked(flow.Window, new RoutedEventArgs());

        flow.VM.IsSaving.Should().BeTrue();
        flow.Front.IsEnabled.Should().BeFalse();
        flow.Back.IsEnabled.Should().BeFalse();
        ((TagInput)flow.Window.FindName("TagsInput")).IsEnabled.Should().BeFalse();
        ((Button)flow.Window.FindName("MoreButton")).IsEnabled.Should().BeFalse();
        flow.Window.SaveButtonClicked(flow.Window, new RoutedEventArgs());
        flow.Window.RevertButtonClicked(flow.Window, new RoutedEventArgs());
        flow.Card.Tags.Should().BeEmpty();
        flow.Card.Deck.Id.Should().Be(9);
        flow.Window.Close();
        flow.Closed.Should().BeFalse();
        flow.Dialogs.Verify(dialog => dialog.ShowWarning(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<DialogButtons>()), Times.Never);

        if (succeeds) completion.SetResult();
        else completion.SetException(new IOException());
        Pump();

        flow.VM.IsSaving.Should().BeFalse();
        flow.Closed.Should().Be(succeeds);
        flow.Service.Verify(service => service.SaveEditedCard(It.IsAny<CardEntity>(), CardAction.Modify, null), Times.Once);
        if (!succeeds)
        {
            flow.Front.IsEnabled.Should().BeTrue();
            flow.Card.Tags.Should().BeEmpty();
            flow.Card.Deck.Id.Should().Be(9);
            flow.Window.Close();
            flow.Closed.Should().BeFalse();
            flow.Dialogs.Verify(dialog => dialog.ShowWarning(It.IsAny<string>(), "Unsaved changes", DialogButtons.YesNo), Times.Once);

            flow.Service.Setup(service => service.SaveEditedCard(It.IsAny<CardEntity>(), CardAction.Modify, null))
                .Returns(Task.CompletedTask);
            flow.Window.SaveButtonClicked(flow.Window, new RoutedEventArgs());
            Pump();
            flow.Closed.Should().BeTrue();
        }
        flow.Dialogs.Verify(dialog => dialog.ShowWarning(It.IsAny<string>(), "Unsaved changes", DialogButtons.YesNo),
            succeeds ? Times.Never() : Times.Once());
    });

    [Fact]
    public Task WhitespaceChanges_AreNotTrimmedOutOfTheCloseCheck() => host.Run(() =>
    {
        using var flow = CreateFlow();
        ((Paragraph)flow.Front.Document.Blocks.FirstBlock).Inlines.Add(new Run(" "));

        flow.Window.Close();

        flow.Closed.Should().BeFalse();
        flow.Dialogs.Verify(dialog => dialog.ShowWarning(It.IsAny<string>(), "Unsaved changes", DialogButtons.YesNo), Times.Once);
    });

    [Theory]
    [InlineData("draft")]
    [InlineData("save")]
    [InlineData("tag")]
    public Task FatalShutdown_ClosesDirtyEditorWithoutPromptOrStartingASave(string state) => host.Run(() =>
    {
        using var flow = CreateFlow();
        new TextRange(flow.Front.Document.ContentStart, flow.Front.Document.ContentEnd).Text = "Unsaved draft";
        var saveCompletion = new TaskCompletionSource();
        var tagCompletion = new TaskCompletionSource<TagVM?>();
        var tagsInput = (TagInput)flow.Window.FindName("TagsInput");
        Task committing = Task.CompletedTask;

        try
        {
            if (state == "save")
            {
                flow.Service.Setup(service => service.SaveEditedCard(It.IsAny<CardEntity>(), CardAction.Modify, null))
                    .Returns(saveCompletion.Task);
                flow.Window.SaveButtonClicked(flow.Window, new RoutedEventArgs());
                flow.VM.IsSaving.Should().BeTrue();
            }
            else if (state == "tag")
            {
                Mock.Get(flow.VM.CardTagsVM).Setup(vm => vm.TryAddTagAsync("Pending tag"))
                    .Returns(tagCompletion.Task);
                tagsInput.DataContext = flow.VM.CardTagsVM;
                tagsInput.Input.Text = "Pending tag";
                committing = tagsInput.CommitAsync();
                tagsInput.IsCommitting.Should().BeTrue();
            }

            bool handled = App.TryHandleFatalException(new Exception("Unexpected UI failure"), NullLogger.Instance,
                Mock.Of<IDialogService>(), () => flow.Window.Close());

            handled.Should().BeTrue();
            flow.Closed.Should().BeTrue();
            flow.VM.IsSaving.Should().Be(state == "save");
            tagsInput.IsCommitting.Should().Be(state == "tag");
            flow.Dialogs.VerifyNoOtherCalls();
            flow.Service.Verify(service => service.SaveEditedCard(It.IsAny<CardEntity>(), CardAction.Modify, null),
                state == "save" ? Times.Once() : Times.Never());
            flow.Service.VerifyNoOtherCalls();
        }
        finally
        {
            // Complete existing operations while the hidden WPF dispatcher is still available.
            saveCompletion.TrySetResult();
            tagCompletion.TrySetResult(null);
            Pump();
            committing.GetAwaiter().GetResult();
            App.IsFatalShutdownStarted = false;
        }
    });

    private static Flow CreateFlow(string blocks = "<Paragraph><Run>front</Run></Paragraph>")
    {
        // Use the app's embedded key without starting App or touching the user's database.
        using (var stream = typeof(App).Assembly.GetManifestResourceStream("FlashMemo.SyncfusionLicense"))
        {
            if (stream is not null)
            {
                using var reader = new StreamReader(stream);
                string key = reader.ReadToEnd().Trim();
                if (!string.IsNullOrWhiteSpace(key)) SyncfusionLicenseProvider.RegisterLicense(key);
            }
        }
        SynchronizationContext.SetSynchronizationContext(new DispatcherSynchronizationContext());
        var saved = new CardEntity
        {
            Id = 1, UserId = 7, DeckId = 2, NoteId = 3,
            Deck = new Deck { Id = 2, UserId = 7, Name = "Deck" },
            Note = new StandardNote
            {
                Id = 3,
                FrontContent = $"<FlowDocument xmlns=\"http://schemas.microsoft.com/winfx/2006/xaml/presentation\">{blocks}</FlowDocument>",
                BackContent = "<FlowDocument xmlns=\"http://schemas.microsoft.com/winfx/2006/xaml/presentation\"><Paragraph>back</Paragraph></FlowDocument>"
            },
            Tags = [new Tag { Id = 4, UserId = 7, Name = "Tag" }]
        };
        var card = new EditableCardVM(saved, new XamlNoteComparer(), Helpers.GetMapper());
        var dialogs = new Mock<IDialogService>(MockBehavior.Strict);
        dialogs.Setup(dialog => dialog.ShowWarning(It.IsAny<string>(), "Unsaved changes", DialogButtons.YesNo))
            .Returns(DialogResult.No);
        dialogs.Setup(dialog => dialog.ShowError(It.IsAny<string>(), "Error", DialogButtons.OK)).Returns(DialogResult.OK);
        var service = new Mock<ICardService>(MockBehavior.Strict);
        var vm = new CardEditorVM(service.Object, null!, new VMEventBus(), null!, card,
            NullLogger<CardEditorVM>.Instance, dialogs.Object, Helpers.GetSafetyNet(dialogs.Object));
        vm.Initialize(null!, Mock.Of<ICardTagsVM>());
        var window = new EditCardWindow { VM = vm };
        new WindowService(null!, null!, null!, null!, null!, null!, null!,
            Helpers.GetSafetyNet(dialogs.Object), NullLogger<WindowService>.Instance)
            .WireEvents(vm, window);
        window.RaiseEvent(new RoutedEventArgs(FrameworkElement.LoadedEvent));
        window.Measure(new Size(900, 650));
        window.Arrange(new Rect(0, 0, 900, 650));
        window.UpdateLayout();
        Pump();
        var flow = new Flow(window, vm, card, service, dialogs);
        window.Closed += (_, _) => flow.Closed = true;
        return flow;
    }

    private static void Pump() => Dispatcher.CurrentDispatcher.Invoke(() => { }, DispatcherPriority.ApplicationIdle);

    private sealed class Flow(EditCardWindow window, CardEditorVM vm, EditableCardVM card,
        Mock<ICardService> service, Mock<IDialogService> dialogs) : IDisposable
    {
        public EditCardWindow Window { get; } = window;
        public CardEditorVM VM { get; } = vm;
        public EditableCardVM Card { get; } = card;
        public Mock<ICardService> Service { get; } = service;
        public Mock<IDialogService> Dialogs { get; } = dialogs;
        public RichTextBox Front => (RichTextBox)Window.FindName("FrontBox");
        public RichTextBox Back => (RichTextBox)Window.FindName("BackBox");
        public bool Closed { get; set; }

        public void Dispose()
        {
            if (Closed) return;
            Dialogs.Setup(dialog => dialog.ShowWarning(It.IsAny<string>(), "Unsaved changes", DialogButtons.YesNo))
                .Returns(DialogResult.Yes);
            Window.Close();
        }
    }
}