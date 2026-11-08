using FlashMemo.Model.Domain;
using FlashMemo.Model.Persistence;
using FlashMemo.Services;
using FlashMemo.ViewModel.Windows;
using FlashMemo.ViewModel.Wrappers;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;

namespace FlashMemo.Tests.VMTests;

public class DeckOptionsPresetWorkflowTests
{
    [Theory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    public async Task CreateOrClone_PublishesOnlyPersistedPresets(bool clone, bool succeeds)
    {
        var flow = await CreateWorkflow();
        flow.VM.BeginCreatePresetCommand.Execute(null);
        flow.VM.NewPresetName = "New preset";
        var completion = new TaskCompletionSource<DeckOptions>();
        DeckOptions? submitted = null;
        flow.Service.Setup(service => service.CreateNew(It.IsAny<DeckOptions>()))
            .Callback<DeckOptions>(preset => submitted = preset)
            .Returns(completion.Task);

        var saving = clone
            ? flow.VM.ClonePresetCommand.ExecuteAsync(null)
            : flow.VM.AddNewPresetCommand.ExecuteAsync(null);

        flow.VM.AllPresets.Should().HaveCount(3);
        flow.VM.CurrentOptions.Should().BeSameAs(flow.Original);
        flow.Deck.OptionsId.Should().Be(11);
        submitted!.Id.Should().Be(0);
        submitted.UserId.Should().Be(7);
        submitted.Name.Should().Be(clone ? "Original - copy" : "New preset");

        if (succeeds) completion.SetResult(submitted with { Id = 13 });
        else completion.SetException(new IOException());
        await saving;

        flow.VM.AllPresets.Should().HaveCount(succeeds ? 4 : 3);
        flow.VM.CurrentOptions.Id.Should().Be(succeeds ? 13 : 11);
        flow.Deck.OptionsId.Should().Be(succeeds ? 13 : 11);
        flow.Original.DeckCount.Should().Be(succeeds ? 1 : 2);
        flow.Service.Verify(service => service.AssignToDeck(It.IsAny<long>(), It.IsAny<long>()), Times.Never);
        if (!succeeds)
        {
            flow.VM.IsCreatingPreset.Should().BeTrue();
            flow.VM.NewPresetName.Should().Be("New preset");
        }
        else if (!clone)
        {
            flow.VM.IsCreatingPreset.Should().BeFalse();
            flow.VM.NewPresetName.Should().BeNull();
        }

        VerifyErrors(flow, succeeds ? 0 : 1);
    }

    [Fact]
    public async Task CloneDefault_CreatesAPresetOwnedByTheCurrentUser()
    {
        var flow = await CreateWorkflow();
        await flow.VM.ChangePresetCommand.ExecuteAsync(flow.Default);
        flow.Service.Setup(service => service.CreateNew(It.IsAny<DeckOptions>()))
            .ReturnsAsync((DeckOptions preset) => preset with { Id = 13 });

        await flow.VM.ClonePresetCommand.ExecuteAsync(null);

        flow.Service.Verify(service => service.CreateNew(It.Is<DeckOptions>(preset =>
            preset.UserId == 7 && preset.Id == 0 && preset.Name == "Default - copy")), Times.Once);
        flow.VM.CurrentOptions.Id.Should().Be(13);
    }

    [Theory]
    [InlineData(DialogResult.No)]
    [InlineData(DialogResult.Cancel)]
    public async Task CreatedPreset_RemainsAvailableWhenSwitchingIsCancelled(DialogResult answer)
    {
        var flow = await CreateWorkflow();
        flow.Original.DailyLimits.Reviews++;
        flow.Dialogs.Setup(dialog => dialog.Show(It.IsAny<string>(), It.IsAny<string>(),
            DialogButtons.YesNoCancel, DialogIcons.Warning)).Returns(answer);
        flow.Service.Setup(service => service.CreateNew(It.IsAny<DeckOptions>()))
            .ReturnsAsync((DeckOptions preset) => preset with { Id = 13 });
        flow.VM.NewPresetName = "New preset";

        await flow.VM.AddNewPresetCommand.ExecuteAsync(null);

        flow.VM.AllPresets.Should().Contain(preset => preset.Id == 13
            && preset.DeckCount == (answer == DialogResult.No ? 1 : 0));
        flow.VM.CurrentOptions.Should().BeSameAs(answer == DialogResult.No
            ? flow.VM.AllPresets.Single(preset => preset.Id == 13)
            : flow.Original);
        flow.Deck.OptionsId.Should().Be(answer == DialogResult.No ? 13 : 11);
        flow.Original.DeckCount.Should().Be(answer == DialogResult.No ? 1 : 2);
        flow.VM.IsCurrentModified.Should().Be(answer == DialogResult.Cancel);
        flow.Service.Verify(service => service.AssignToDeck(It.IsAny<long>(), It.IsAny<long>()), Times.Never);
        flow.Bus.Verify(bus => bus.NotifyDeckOpt(), Times.Never);
        VerifyErrors(flow, 0);
    }

    [Fact]
    public async Task SaveAfterSelection_PersistsTheNewPresetAssignment()
    {
        var flow = await CreateWorkflow();
        flow.Service.Setup(service => service.SaveEditedPreset(It.IsAny<DeckOptions>()))
            .Returns(Task.CompletedTask);

        await flow.VM.ChangePresetCommand.ExecuteAsync(flow.Other);
        await flow.VM.SaveChangesCommand.ExecuteAsync(null);

        flow.Service.Verify(service => service.SaveEditedPreset(It.Is<DeckOptions>(preset => preset.Id == 12)),
            Times.Once);
        flow.Service.Verify(service => service.AssignToDeck(1, 12), Times.Once);
    }

    [Fact]
    public async Task Switch_ChangesSelectionLocallyAndClosingDoesNotPersistIt()
    {
        var flow = await CreateWorkflow();

        await flow.VM.ChangePresetCommand.ExecuteAsync(flow.Other);

        flow.VM.CurrentOptions.Should().BeSameAs(flow.Other);
        flow.Deck.OptionsId.Should().Be(12);
        flow.Original.DeckCount.Should().Be(1);
        flow.Other.DeckCount.Should().Be(1);
        (await flow.VM.CanCloseAsync()).Should().BeTrue();
        flow.Service.Verify(service => service.AssignToDeck(It.IsAny<long>(), It.IsAny<long>()), Times.Never);
        flow.Bus.Verify(bus => bus.NotifyDeckOpt(), Times.Never);
        VerifyErrors(flow, 0);
    }

    [Fact]
    public async Task SelectingCurrentPreset_DoesNotWriteOrChangeCounts()
    {
        var flow = await CreateWorkflow();

        await flow.VM.ChangePresetCommand.ExecuteAsync(flow.Original);

        flow.Original.DeckCount.Should().Be(2);
        flow.Service.Verify(service => service.AssignToDeck(It.IsAny<long>(), It.IsAny<long>()), Times.Never);
        flow.Bus.Verify(bus => bus.NotifyDeckOpt(), Times.Never);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    public async Task Switch_SaveFirstPersistsOnlyTheOldPresetAssignment(int outcome)
    {
        var flow = await CreateWorkflow();
        int savedReviews = flow.Original.DailyLimits.Reviews;
        flow.Original.DailyLimits.Reviews++;
        flow.Dialogs.Setup(dialog => dialog.Show(It.IsAny<string>(), It.IsAny<string>(),
            DialogButtons.YesNoCancel, DialogIcons.Warning)).Returns(DialogResult.Yes);
        var calls = new List<string>();
        flow.Service.Setup(service => service.SaveEditedPreset(It.IsAny<DeckOptions>()))
            .Callback(() => calls.Add("save"))
            .Returns(outcome == 0 ? Task.FromException(new IOException()) : Task.CompletedTask);
        flow.Service.Setup(service => service.AssignToDeck(1, 11))
            .Callback(() => calls.Add("assign current"))
            .Returns(outcome == 1 ? Task.FromException(new IOException()) : Task.CompletedTask);
        await flow.VM.ChangePresetCommand.ExecuteAsync(flow.Other);

        calls.Should().Equal(outcome switch
        {
            0 => ["save"],
            _ => ["save", "assign current"]
        });
        flow.VM.CurrentOptions.Should().BeSameAs(outcome == 2 ? flow.Other : flow.Original);
        flow.Deck.OptionsId.Should().Be(outcome == 2 ? 12 : 11);
        flow.Original.DeckCount.Should().Be(outcome == 2 ? 1 : 2);
        flow.Other.DeckCount.Should().Be(outcome == 2 ? 1 : 0);
        flow.Original.DailyLimits.Reviews.Should().Be(savedReviews + 1);
        flow.VM.IsCurrentModified.Should().Be(outcome == 0);
        VerifyErrors(flow, outcome == 2 ? 0 : 1);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Remove_CommitsSelectionAndAllAffectedCountsOnlyAfterSuccess(bool succeeds)
    {
        var flow = await CreateWorkflow();
        flow.Original.DailyLimits.Reviews++;
        var snapshot = flow.VM.GetPrivateField<DeckOptions>("lastSaved");
        var completion = new TaskCompletionSource();
        flow.Service.Setup(service => service.Remove(11)).Returns(completion.Task);

        var removing = flow.VM.RemovePresetCommand.ExecuteAsync(null);

        flow.VM.AllPresets.Should().Contain(flow.Original);
        flow.VM.CurrentOptions.Should().BeSameAs(flow.Original);
        flow.Deck.OptionsId.Should().Be(11);
        flow.Default.DeckCount.Should().Be(1);
        flow.VM.GetPrivateField<DeckOptions>("lastSaved").Should().BeSameAs(snapshot);

        if (succeeds) completion.SetResult();
        else completion.SetException(new IOException());
        await removing;

        flow.VM.AllPresets.Contains(flow.Original).Should().Be(!succeeds);
        flow.VM.CurrentOptions.Should().BeSameAs(succeeds ? flow.Default : flow.Original);
        flow.Deck.OptionsId.Should().Be(succeeds ? -1 : 11);
        flow.Default.DeckCount.Should().Be(succeeds ? 3 : 1);
        flow.VM.IsCurrentModified.Should().Be(!succeeds);
        flow.Bus.Verify(bus => bus.NotifyDeckOpt(), succeeds ? Times.Once() : Times.Never());
        VerifyErrors(flow, succeeds ? 0 : 1);
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    public async Task DeletionPreferences_FailedReadLogsAndStillAsks(bool readFails, bool confirm)
    {
        var flow = await CreateWorkflow();
        flow.Preferences.Setup(service => service.GetFromUser(7))
            .Returns(readFails ? Task.FromException<UserOptions>(new IOException())
                : Task.FromResult(UserOptions.CreateDefault() with { ConfirmPresetDeletion = confirm }));
        flow.Dialogs.Setup(dialog => dialog.Show("Delete preset?", It.IsAny<string>(),
            DialogButtons.YesNo, DialogIcons.Warning)).Returns(DialogResult.No);

        (await flow.VM.CanRemovePreset()).Should().Be(!readFails && !confirm);

        flow.Dialogs.Verify(dialog => dialog.Show("Delete preset?", It.IsAny<string>(),
            DialogButtons.YesNo, DialogIcons.Warning), readFails || confirm ? Times.Once() : Times.Never());
        flow.Logger.Verify(logger => logger.Log(LogLevel.Error, It.IsAny<EventId>(),
            It.IsAny<It.IsAnyType>(), It.IsAny<Exception>(),
            It.IsAny<Func<It.IsAnyType, Exception?, string>>()), readFails ? Times.Once() : Times.Never());
        VerifyErrors(flow, 0);
    }

    [Theory]
    [InlineData(DialogResult.No)]
    [InlineData(DialogResult.Yes)]
    public async Task Remove_PreferenceFailureStillHonorsTheDeletionAnswer(DialogResult answer)
    {
        var flow = await CreateWorkflow();
        flow.Preferences.Setup(service => service.GetFromUser(7)).ThrowsAsync(new IOException());
        flow.Dialogs.Setup(dialog => dialog.Show("Delete preset?", It.IsAny<string>(),
            DialogButtons.YesNo, DialogIcons.Warning)).Returns(answer);
        flow.Service.Setup(service => service.Remove(11)).Returns(Task.CompletedTask);

        await flow.VM.RemovePresetCommand.ExecuteAsync(null);

        flow.Service.Verify(service => service.Remove(11), answer == DialogResult.Yes ? Times.Once() : Times.Never());
        flow.VM.AllPresets.Contains(flow.Original).Should().Be(answer != DialogResult.Yes);
        VerifyErrors(flow, 0);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Rename_SubmitsDraftAndUpdatesOnlyTheSavedNameAfterSuccess(bool succeeds)
    {
        var flow = await CreateWorkflow();
        flow.Original.DailyLimits.Reviews++;
        flow.Original.BeginRenameCommand.Execute(null);
        flow.Original.TempName = "Renamed";
        var completion = new TaskCompletionSource();
        flow.Service.Setup(service => service.Rename("Renamed", 11)).Returns(completion.Task);

        var renaming = flow.VM.RenamePresetCommand.ExecuteAsync(flow.Original);

        flow.Original.Name.Should().Be("Original");
        flow.VM.GetPrivateField<DeckOptions>("lastSaved").Name.Should().Be("Original");
        flow.Original.CancelRenameCommand.Execute(null);
        flow.Original.IsRenaming.Should().BeTrue();

        if (succeeds) completion.SetResult();
        else completion.SetException(new IOException());
        await renaming;

        flow.Service.Verify(service => service.Rename("Renamed", 11), Times.Once);
        flow.Original.Name.Should().Be(succeeds ? "Renamed" : "Original");
        flow.Original.TempName.Should().Be(succeeds ? null : "Renamed");
        flow.Original.IsRenaming.Should().Be(!succeeds);
        flow.VM.GetPrivateField<DeckOptions>("lastSaved").Name.Should().Be(succeeds ? "Renamed" : "Original");
        flow.VM.IsCurrentModified.Should().BeTrue(); // Renaming must not mark the other edits saved.
        flow.Bus.Verify(bus => bus.NotifyDeckOpt(), succeeds ? Times.Once() : Times.Never());
        VerifyErrors(flow, succeeds ? 0 : 1);
    }

    private static void VerifyErrors(Workflow flow, int count)
        => flow.Dialogs.Verify(dialog => dialog.ShowError(It.IsAny<string>(), "Error", DialogButtons.OK),
            Times.Exactly(count));

    private static async Task<Workflow> CreateWorkflow()
    {
        var mapper = Helpers.GetMapper();
        var original = mapper.Map<DeckOptionsVM>(DeckOptions.CreateNew("Original", 7) with { Id = 11 });
        original.DeckCount = 2;
        var other = mapper.Map<DeckOptionsVM>(DeckOptions.CreateNew("Other", 7) with { Id = 12 });
        var defaultPreset = mapper.Map<DeckOptionsVM>(DeckOptions.Default);
        defaultPreset.DeckCount = 1;
        var builder = new Mock<IDeckOptVMBuilder>();
        builder.Setup(service => service.BuildAllCounted(7))
            .ReturnsAsync(new List<DeckOptionsVM> { defaultPreset, original, other });
        var service = new Mock<IDeckOptionsService>(MockBehavior.Strict);
        service.Setup(repo => repo.AssignToDeck(1, It.IsAny<long>())).Returns(Task.CompletedTask);
        var preferences = new Mock<IUserOptionsService>();
        preferences.Setup(repo => repo.GetFromUser(7))
            .ReturnsAsync(UserOptions.CreateDefault() with { ConfirmPresetDeletion = false });
        var dialogs = new Mock<IDialogService>();
        dialogs.Setup(dialog => dialog.Show(It.IsAny<string>(), It.IsAny<string>(),
            DialogButtons.YesNoCancel, DialogIcons.Question)).Returns(DialogResult.No);
        var bus = new Mock<IVMEventBus>();
        var logger = new Mock<ILogger<DeckOptionsMenuVM>>();
        var deck = new Deck { Id = 1, UserId = 7, OptionsId = 11 };
        var vm = new DeckOptionsMenuVM(mapper, builder.Object, service.Object, preferences.Object,
            deck, bus.Object, logger.Object, dialogs.Object, Helpers.GetSafetyNet(dialogs.Object));
        await vm.InitializeAsync();
        return new(vm, deck, original, other, defaultPreset, service, preferences, dialogs, bus, logger);
    }

    private record Workflow(DeckOptionsMenuVM VM, Deck Deck, DeckOptionsVM Original,
        DeckOptionsVM Other, DeckOptionsVM Default, Mock<IDeckOptionsService> Service,
        Mock<IUserOptionsService> Preferences, Mock<IDialogService> Dialogs, Mock<IVMEventBus> Bus,
        Mock<ILogger<DeckOptionsMenuVM>> Logger);
}