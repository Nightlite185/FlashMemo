using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Threading;
using FlashMemo.Model.Persistence;
using FlashMemo.Repositories;
using FlashMemo.Services;
using FlashMemo.View;
using FlashMemo.ViewModel;
using FlashMemo.ViewModel.Factories;
using FlashMemo.ViewModel.Other;
using FlashMemo.ViewModel.Wrappers;
using FluentAssertions;
using Moq;
using Syncfusion.Licensing;
using Syncfusion.Windows.Controls.Input;

namespace FlashMemo.Tests.ViewTests;

[Collection("WPF")]
public class TagInputTests(WpfTestHost host)
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public Task Enter_SavesBeforePublishingAndKeepsFailedTextAvailable(bool succeeds) => host.Run(() =>
    {
        using var flow = CreateFlow();
        flow.Input.Text = "  New tag  ";
        var completion = new TaskCompletionSource<Tag>();
        flow.Repo.Setup(repo => repo.GetOrCreate(It.Is<Tag>(tag => tag.Name == "New tag")))
            .Returns(completion.Task);

        Press(flow, Key.Enter).Handled.Should().BeTrue();

        flow.Control.IsCommitting.Should().BeTrue();
        flow.Input.IsEnabled.Should().BeFalse();
        flow.VM.SelectedTags.Should().ContainSingle();
        flow.Host.Tags.Should().ContainSingle();
        Press(flow, Key.Enter);
        flow.Repo.Verify(repo => repo.GetOrCreate(It.IsAny<Tag>()), Times.Once);

        var saved = new Tag { Id = 4, UserId = 7, Name = "New tag" };
        if (succeeds) completion.SetResult(saved);
        else completion.SetException(new IOException());
        Pump();

        flow.Control.IsCommitting.Should().BeFalse();
        flow.Input.IsEnabled.Should().BeTrue();
        if (!succeeds)
        {
            flow.Input.Text.Should().Be("  New tag  ");
            flow.Host.Tags.Should().ContainSingle();
            flow.VM.SelectedTags.Should().ContainSingle();
            Tokens(flow).Should().ContainSingle();
            flow.Dialogs.Verify(dialog => dialog.ShowError(
                It.IsAny<string>(), "Error", DialogButtons.OK), Times.Once);
            flow.Repo.Setup(repo => repo.GetOrCreate(It.IsAny<Tag>())).ReturnsAsync(saved);
            Press(flow, Key.Enter);
            Pump();
        }

        flow.Input.Text.Should().BeEmpty();
        flow.VM.SelectedTags.Select(tag => tag.Id).Should().Equal(1, 4);
        flow.Host.Tags.Select(tag => tag.Id).Should().Equal(1, 4);
        Tokens(flow).Should().HaveCount(2);
    });

    [Fact]
    public Task HighlightedSuggestion_IsCommittedOnce() => host.Run(() =>
    {
        using var flow = CreateFlow();
        flow.Input.Text = "Gam";
        flow.Input.FilterSuggestions();
        flow.Input.IsSuggestionOpen = true;
        Press(flow, Key.Down);

        Press(flow, Key.Enter).Handled.Should().BeTrue();
        Pump();

        flow.Input.Text.Should().BeEmpty();
        flow.Host.Tags.Select(tag => tag.Id).Should().Equal(1, 3);
        Tokens(flow).Should().HaveCount(2);
        flow.Repo.Verify(repo => repo.GetOrCreate(It.IsAny<Tag>()), Times.Never);
        flow.Dialogs.VerifyNoOtherCalls();
    });

    [Fact]
    public Task DuplicateEnter_DoesNotAddOrRenameTheSharedTag() => host.Run(() =>
    {
        using var flow = CreateFlow();
        flow.Input.Text = "ALPHA";

        Press(flow, Key.Enter);
        Pump();

        flow.Input.Text.Should().BeEmpty();
        flow.Host.Tags.Should().ContainSingle().Which.Name.Should().Be("Alpha");
        Tokens(flow).Should().ContainSingle();
        flow.Repo.Verify(repo => repo.GetOrCreate(It.IsAny<Tag>()), Times.Never);
    });

    [Fact]
    public Task NativeCloseButton_RemovesOnlyTheCardSelection() => host.Run(() =>
    {
        using var flow = CreateFlow();
        var token = Tokens(flow).Single();
        var button = Descendants(token).OfType<Button>().Single(item => item.Name == "TokenCloseButton");

        button.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
        Pump();

        flow.VM.SelectedTags.Should().BeEmpty();
        flow.Host.Tags.Should().BeEmpty();
        Tokens(flow).Should().BeEmpty();
        flow.VM.AllTags.Should().HaveCount(3);
        flow.Repo.Verify(repo => repo.Remove(It.IsAny<long>()), Times.Never);
    });

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public Task Refresh_ReflectsNewOrRevertedDraftWithoutLosingSuggestions(bool revert) => host.Run(() =>
    {
        using var flow = CreateFlow();
        flow.Host.Tags = revert
            ? [new TagVM(new Tag { Id = 2, UserId = 7, Name = "Beta" })]
            : [];
        flow.Input.Text = "Uncommitted";

        flow.Control.Refresh();
        Pump();

        flow.Input.Text.Should().BeEmpty();
        flow.VM.SelectedTags.Select(tag => tag.Id).Should().Equal(revert ? new long[] { 2 } : []);
        flow.Host.Tags.Select(tag => tag.Id).Should().Equal(revert ? new long[] { 2 } : []);
        Tokens(flow).Should().HaveCount(revert ? 1 : 0);
        flow.VM.AllTags.Should().HaveCount(3);
    });

    private static Flow CreateFlow()
    {
        // Register the same embedded key as the app without starting App or running migrations.
        using (var stream = typeof(App).Assembly.GetManifestResourceStream("FlashMemo.SyncfusionLicense"))
        {
            if (stream is not null)
            {
                using var reader = new StreamReader(stream);
                string key = reader.ReadToEnd().Trim();
                if (!string.IsNullOrWhiteSpace(key))
                    SyncfusionLicenseProvider.RegisterLicense(key);
            }
        }
        SynchronizationContext.SetSynchronizationContext(new DispatcherSynchronizationContext());
        Tag[] tags =
        [
            new() { Id = 1, UserId = 7, Name = "Alpha" },
            new() { Id = 2, UserId = 7, Name = "Beta" },
            new() { Id = 3, UserId = 7, Name = "Gamma" }
        ];
        var repo = new Mock<ITagRepo>(MockBehavior.Strict);
        repo.Setup(value => value.GetFromUser(7)).ReturnsAsync(tags);
        var tagHost = new TagHost();
        tagHost.Tags.Add(new TagVM(tags[0]));
        var dialogs = new Mock<IDialogService>(MockBehavior.Strict);
        dialogs.Setup(dialog => dialog.ShowError(It.IsAny<string>(), "Error", DialogButtons.OK))
            .Returns(DialogResult.OK);
        var vm = new CardTagsVMF(repo.Object, Mock.Of<IVMEventBus>(), Helpers.GetSafetyNet(dialogs.Object))
            .CreateAsync(7, tagHost).GetAwaiter().GetResult();
        var control = new TagInput { DataContext = vm, Width = 500 };
        var input = (SfTextBoxExt)control.FindName("Input");
        var source = new HwndSource(new HwndSourceParameters("FlashMemo tag input test")
        {
            Width = 600, Height = 200, WindowStyle = unchecked((int)0x80000000)
        });
        source.RootVisual = control; // Hidden host: no visible window or desktop interaction.
        control.Measure(new Size(500, 200));
        control.Arrange(new Rect(0, 0, 500, 200));
        control.UpdateLayout();
        Pump();
        ReferenceEquals(input.SelectedItems, vm.SelectedTags).Should().BeTrue();
        return new(control, input, source, vm, tagHost, repo, dialogs);
    }

    private static KeyEventArgs Press(Flow flow, Key key)
    {
        var args = new KeyEventArgs(Keyboard.PrimaryDevice, flow.Source, Environment.TickCount, key)
        {
            RoutedEvent = Keyboard.PreviewKeyDownEvent
        };
        flow.Input.RaiseEvent(args);
        return args;
    }

    private static TokenItem[] Tokens(Flow flow) => Descendants(flow.Input).OfType<TokenItem>().ToArray();

    private static IEnumerable<DependencyObject> Descendants(DependencyObject parent)
    {
        for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
        {
            var child = VisualTreeHelper.GetChild(parent, i);
            yield return child;
            foreach (var descendant in Descendants(child))
                yield return descendant;
        }
    }

    private static void Pump()
    {
        var frame = new DispatcherFrame();
        Dispatcher.CurrentDispatcher.BeginInvoke(DispatcherPriority.ApplicationIdle,
            (Action)(() => frame.Continue = false));
        Dispatcher.PushFrame(frame);
    }

    private sealed class TagHost : ICardTagsVMHost
    {
        public List<TagVM> Tags { get; set; } = [];
    }

    private sealed record Flow(TagInput Control, SfTextBoxExt Input, HwndSource Source,
        CardTagsVM VM, TagHost Host, Mock<ITagRepo> Repo, Mock<IDialogService> Dialogs) : IDisposable
    {
        public void Dispose() => Source.Dispose();
    }
}