using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using FlashMemo.Helpers;
using FlashMemo.Model.Persistence;
using FlashMemo.Repositories;
using FlashMemo.Services;
using FlashMemo.ViewModel;
using FlashMemo.ViewModel.Factories;
using FlashMemo.ViewModel.Wrappers;
using FluentAssertions;
using Moq;

namespace FlashMemo.Tests.ViewTests;

public class TagChipInputControllerTests
{
    [Theory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    public Task Commit_PreservesDraftUntilSavedAndBlocksOverlappingInput(bool replace, bool succeeds)
        => Helpers.RunOnSta(async () =>
        {
            var flow = await CreateFlow();
            if (replace)
                ClickChip(flow, 1, remove: false);
            flow.Input.Text = "Replacement";
            var completion = new TaskCompletionSource<Tag>();
            flow.Repo.Setup(repo => repo.GetOrCreate(It.IsAny<Tag>())).Returns(completion.Task);

            var saving = Commit(flow, "Replacement");

            flow.Input.IsReadOnly.Should().BeTrue();
            flow.Host.Tags.Select(tag => tag.Id).Should().Equal(1, 2);
            await Commit(flow, "Replacement");
            ClickChip(flow, 2, remove: false);
            ClickChip(flow, 2, remove: true);
            flow.Input.Text.Should().Be("Replacement");
            flow.Host.Tags.Select(tag => tag.Id).Should().Equal(1, 2);
            flow.Repo.Verify(repo => repo.GetOrCreate(It.IsAny<Tag>()), Times.Once);

            var replacement = new Tag { Id = 3, UserId = 7, Name = "Replacement" };
            if (succeeds) completion.SetResult(replacement);
            else completion.SetException(new IOException());
            await saving;

            flow.Input.IsReadOnly.Should().BeFalse();
            if (!succeeds)
            {
                flow.Input.Text.Should().Be("Replacement");
                flow.Host.Tags.Select(tag => tag.Id).Should().Equal(1, 2);
                ChipIds(flow).Should().BeEquivalentTo(replace ? new long[] { 2 } : [1, 2]);
                flow.Dialogs.Verify(dialog => dialog.ShowError(
                    It.IsAny<string>(), "Error", DialogButtons.OK), Times.Once);

                // The original opened chip and typed replacement remain usable for another attempt.
                flow.Repo.Setup(repo => repo.GetOrCreate(It.IsAny<Tag>())).ReturnsAsync(replacement);
                await Commit(flow, "Replacement");
            }

            flow.Input.Text.Should().BeEmpty();
            flow.Host.Tags.Select(tag => tag.Id).Should().BeEquivalentTo(replace ? new long[] { 2, 3 } : [1, 2, 3]);
            ChipIds(flow).Should().BeEquivalentTo(flow.Host.Tags.Select(tag => tag.Id));
            flow.Input.IsReadOnly.Should().BeFalse();
            flow.Bus.Verify(bus => bus.NotifyDomain(), Times.Once);
        });

    [Fact]
    public Task ReplacingWithAnAlreadySelectedTag_MergesTheSelection()
        => Helpers.RunOnSta(async () =>
        {
            var flow = await CreateFlow();
            ClickChip(flow, 1, remove: false);
            flow.Input.Text = "OTHER";

            await Commit(flow, "OTHER");

            flow.Host.Tags.Should().ContainSingle().Which.Id.Should().Be(2);
            ChipIds(flow).Should().Equal(2);
            flow.Input.Text.Should().BeEmpty();
            flow.Repo.Verify(repo => repo.GetOrCreate(It.IsAny<Tag>()), Times.Never);
            flow.Dialogs.VerifyNoOtherCalls();
        });

    [Theory]
    [InlineData("")]
    [InlineData("ORIGINAL")]
    public Task UnchangedChip_KeepsItsStoredSpellingWithoutWriting(string enteredText)
        => Helpers.RunOnSta(async () =>
        {
            var flow = await CreateFlow();
            ClickChip(flow, 1, remove: false);
            flow.Input.Text = enteredText;

            await Commit(flow, enteredText);

            flow.Host.Tags[0].Name.Should().Be("Original");
            ChipIds(flow).Should().Equal(1, 2);
            flow.Input.Text.Should().BeEmpty();
            flow.Repo.Verify(repo => repo.GetOrCreate(It.IsAny<Tag>()), Times.Never);
            flow.Dialogs.VerifyNoOtherCalls();
        });

    [Fact]
    public Task StaleSuggestions_ReuseThePersistedTagAndRefreshTheSuggestionsCache()
        => Helpers.RunOnSta(async () =>
        {
            var flow = await CreateFlow();
            var existing = new Tag { Id = 3, UserId = 7, Name = "Stored spelling" };
            flow.Repo.Setup(repo => repo.GetOrCreate(It.IsAny<Tag>())).ReturnsAsync(existing);
            flow.Input.Text = "stored spelling";

            await Commit(flow, "stored spelling");

            flow.Host.Tags.Last().Id.Should().Be(existing.Id);
            flow.Host.Tags.Last().Name.Should().Be("Stored spelling");
            flow.Controller.GetPrivateField<List<TagVM>>("allTags")
                .Should().Contain(tag => tag.Id == existing.Id);
            flow.Dialogs.VerifyNoOtherCalls();
        });

    private static async Task<Flow> CreateFlow()
    {
        Tag[] tags =
        [
            new() { Id = 1, UserId = 7, Name = "Original" },
            new() { Id = 2, UserId = 7, Name = "Other" }
        ];
        var repo = new Mock<ITagRepo>(MockBehavior.Strict);
        repo.Setup(value => value.GetFromUser(7)).ReturnsAsync(tags);
        var host = new TagHost();
        host.Tags.AddRange(tags.Select(tag => new TagVM(tag)));
        var dialogs = new Mock<IDialogService>(MockBehavior.Strict);
        dialogs.Setup(dialog => dialog.ShowError(It.IsAny<string>(), "Error", DialogButtons.OK))
            .Returns(DialogResult.OK);
        var bus = new Mock<IVMEventBus>();
        var vm = await new CardTagsVMF(repo.Object, bus.Object, Helpers.GetSafetyNet(dialogs.Object))
            .CreateAsync(7, host);
        var panel = new WrapPanel();
        var input = new TextBox();
        var controller = new TagChipInputController(vm, new Border { Child = panel }, panel,
            input, new Popup(), new ListBox());
        await controller.InitializeAsync();
        return new(controller, input, panel, host, repo, dialogs, bus);
    }

    private static Task Commit(Flow flow, string name)
        => (Task)typeof(TagChipInputController)
            .GetMethod("CommitTagAsync", BindingFlags.Instance | BindingFlags.NonPublic)!
            .Invoke(flow.Controller, [name])!;

    private static void ClickChip(Flow flow, long id, bool remove)
    {
        var chip = flow.Panel.Children.OfType<Border>().Single(border => ((TagVM)border.Tag).Id == id);
        var buttons = ((StackPanel)chip.Child).Children.OfType<Button>().ToArray();
        buttons[remove ? 1 : 0].RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
    }

    private static IEnumerable<long> ChipIds(Flow flow)
        => flow.Panel.Children.OfType<Border>().Select(border => ((TagVM)border.Tag).Id);

    private sealed class TagHost : ICardTagsVMHost
    {
        public List<TagVM> Tags { get; } = [];
    }

    private record Flow(TagChipInputController Controller, TextBox Input, WrapPanel Panel,
        TagHost Host, Mock<ITagRepo> Repo, Mock<IDialogService> Dialogs, Mock<IVMEventBus> Bus);
}