using FlashMemo.Model.Persistence;
using FlashMemo.Repositories;
using FlashMemo.Services;
using FlashMemo.ViewModel;
using FlashMemo.ViewModel.Factories;
using FlashMemo.ViewModel.Other;
using FlashMemo.ViewModel.Wrappers;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;

namespace FlashMemo.Tests.VMTests;

public class CardTagsVMTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Add_PublishesOnlyAfterSaveAndPreservesStateOnFailure(bool succeeds)
    {
        var flow = await CreateWorkflow();
        var completion = new TaskCompletionSource<Tag>();
        flow.Repo.Setup(repo => repo.GetOrCreate(It.Is<Tag>(tag => tag.Name == "New" && tag.UserId == 7)))
            .Returns(completion.Task);

        var saving = flow.VM.TryAddTagAsync("  New  ");

        flow.Host.Tags.Should().BeEmpty();
        flow.VM.AllTags.Should().BeEmpty();
        flow.Bus.Verify(bus => bus.NotifyDomain(), Times.Never);
        (await flow.VM.TryAddTagAsync("New")).Should().BeNull();
        flow.Repo.Verify(repo => repo.GetOrCreate(It.IsAny<Tag>()), Times.Once);

        var persisted = new Tag { Id = 12, UserId = 7, Name = "New" };
        if (succeeds) completion.SetResult(persisted);
        else completion.SetException(new IOException());
        var result = await saving;

        if (succeeds)
        {
            result!.Id.Should().Be(12);
            flow.Host.Tags.Should().ContainSingle().Which.Should().BeSameAs(result);
            flow.VM.AllTags.Should().ContainSingle().Which.Should().BeSameAs(result);
        }
        else
        {
            result.Should().BeNull();
            flow.Host.Tags.Should().BeEmpty();
            flow.VM.AllTags.Should().BeEmpty();

            flow.Repo.Setup(repo => repo.GetOrCreate(It.IsAny<Tag>())).ReturnsAsync(persisted);
            (await flow.VM.TryAddTagAsync("New"))!.Id.Should().Be(12);
            flow.Host.Tags.Should().ContainSingle();
        }

        flow.Bus.Verify(bus => bus.NotifyDomain(), Times.Once);
        flow.Dialogs.Verify(dialog => dialog.ShowError(It.IsAny<string>(), "Error", DialogButtons.OK),
            succeeds ? Times.Never() : Times.Once());
        flow.Logger.Verify(logger => logger.Log(LogLevel.Error, It.IsAny<EventId>(),
            It.IsAny<It.IsAnyType>(), It.IsAny<Exception>(),
            It.IsAny<Func<It.IsAnyType, Exception?, string>>()), succeeds ? Times.Never() : Times.Once());
    }

    [Fact]
    public async Task CachedAndAlreadySelectedTags_AreReusedWithoutWritingOrDuplicating()
    {
        var tag = new Tag { Id = 12, UserId = 7, Name = "Stored spelling" };
        var flow = await CreateWorkflow(tag);

        var first = await flow.VM.TryAddTagAsync("stored spelling");
        var second = await flow.VM.TryAddTagAsync("STORED SPELLING");

        first.Should().BeSameAs(second);
        first!.Name.Should().Be("Stored spelling");
        flow.Host.Tags.Should().ContainSingle();
        flow.VM.AllTags.Should().ContainSingle();
        flow.Repo.Verify(repo => repo.GetOrCreate(It.IsAny<Tag>()), Times.Never);
        flow.Bus.Verify(bus => bus.NotifyDomain(), Times.Once);
        flow.Dialogs.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task BlankTag_IsNotSubmitted()
    {
        var flow = await CreateWorkflow();

        (await flow.VM.TryAddTagAsync("  ")).Should().BeNull();

        flow.Repo.Verify(repo => repo.GetOrCreate(It.IsAny<Tag>()), Times.Never);
        flow.Host.Tags.Should().BeEmpty();
        flow.Bus.VerifyNoOtherCalls();
        flow.Dialogs.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task InitializationFailure_PropagatesToTheWindowFactoryBoundary()
    {
        var repo = new Mock<ITagRepo>();
        repo.Setup(value => value.GetFromUser(7)).ThrowsAsync(new IOException());
        var dialogs = new Mock<IDialogService>(MockBehavior.Strict);
        var factory = new CardTagsVMF(repo.Object, Mock.Of<IVMEventBus>(), Helpers.GetSafetyNet(dialogs.Object));

        Func<Task> create = () => factory.CreateAsync(7, new TagHost());
        await create.Should().ThrowAsync<IOException>();

        dialogs.VerifyNoOtherCalls();
    }

    private static async Task<Workflow> CreateWorkflow(params Tag[] cached)
    {
        var repo = new Mock<ITagRepo>(MockBehavior.Strict);
        repo.Setup(value => value.GetFromUser(7)).ReturnsAsync(cached);
        var bus = new Mock<IVMEventBus>();
        var logger = new Mock<ILogger<SafetyNet>>();
        var dialogs = new Mock<IDialogService>(MockBehavior.Strict);
        dialogs.Setup(dialog => dialog.ShowError(It.IsAny<string>(), "Error", DialogButtons.OK))
            .Returns(DialogResult.OK);
        var host = new TagHost();
        var factory = new CardTagsVMF(repo.Object, bus.Object, new SafetyNet(logger.Object, dialogs.Object));
        var vm = await factory.CreateAsync(7, host);
        return new(vm, host, repo, bus, dialogs, logger);
    }

    private sealed class TagHost : ICardTagsVMHost
    {
        public List<TagVM> Tags { get; } = [];
    }

    private record Workflow(CardTagsVM VM, TagHost Host, Mock<ITagRepo> Repo,
        Mock<IVMEventBus> Bus, Mock<IDialogService> Dialogs, Mock<ILogger<SafetyNet>> Logger);
}