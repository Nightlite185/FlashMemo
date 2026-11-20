using FlashMemo.Model.Domain;
using FlashMemo.Model.Persistence;
using FlashMemo.Services;
using FlashMemo.ViewModel.Windows;
using FlashMemo.ViewModel.Wrappers;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace FlashMemo.Tests.VMTests;

public class OptionsSaveWorkflowTests
{
    [Theory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    public async Task UserOptions_SaveFailureKeepsWindowOpenAndSavedSnapshot(bool succeeds, bool fromClosing)
    {
        var service = new Mock<IUserOptionsService>();
        var original = UserOptions.CreateDefault();
        service.Setup(repo => repo.GetFromUser(7)).ReturnsAsync(original);
        service.Setup(repo => repo.Update(7, It.IsAny<UserOptions>()))
            .Returns(succeeds ? Task.CompletedTask : Task.FromException(new IOException()));
        var bus = new Mock<IVMEventBus>();
        var dialogs = new Mock<IDialogService>();
        dialogs.Setup(dialog => dialog.ShowWarning(It.IsAny<string>(), It.IsAny<string>(), DialogButtons.YesNoCancel)).Returns(DialogResult.Yes);
        var vm = new UserOptionsMenuVM(7, service.Object, Helpers.GetMapper(), bus.Object,
            NullLogger<UserOptionsMenuVM>.Instance, dialogs.Object, Helpers.GetSafetyNet(dialogs.Object));
        await vm.InitAsync();
        vm.Options.ShowHeatmap = !original.ShowHeatmap;
        bool closed = false;
        vm.OnCloseRequest += () => closed = true;

        if (fromClosing)
            (await vm.CanCloseAsync()).Should().Be(succeeds);
        else
            await vm.SaveChangesCommand.ExecuteAsync(null);

        closed.Should().Be(succeeds && !fromClosing);
        vm.Options.ShowHeatmap.Should().Be(!original.ShowHeatmap);
        vm.GetPrivateField<UserOptions>("lastSaved").ShowHeatmap
            .Should().Be(succeeds ? !original.ShowHeatmap : original.ShowHeatmap);
        bus.Verify(events => events.NotifyUserOpt(), succeeds ? Times.Once() : Times.Never());
        service.Verify(repo => repo.Update(7, It.IsAny<UserOptions>()), Times.Once);
        dialogs.Verify(dialog => dialog.ShowError(It.IsAny<string>(), "Error", DialogButtons.OK),
            succeeds ? Times.Never() : Times.Once());
    }

    [Theory]
    [InlineData(0, false)]
    [InlineData(0, true)]
    [InlineData(1, false)]
    [InlineData(1, true)]
    [InlineData(2, false)]
    [InlineData(2, true)]
    public async Task PresetSave_ReportsEachStageAndClosesOnlyAfterBothSucceed(int outcome, bool fromClosing)
    {
        var flow = await CreatePresetWorkflow(outcome);
        var current = flow.VM.CurrentOptions;
        int savedReviews = current.DailyLimits.Reviews;
        current.DailyLimits.Reviews++;
        bool closed = false;
        flow.VM.OnCloseRequest += () => closed = true;

        if (fromClosing)
            (await flow.VM.CanCloseAsync()).Should().Be(outcome == 2);
        else
            await flow.VM.SaveChangesCommand.ExecuteAsync(null);

        closed.Should().Be(outcome == 2 && !fromClosing);
        flow.VM.CurrentOptions.Should().BeSameAs(current);
        flow.Calls.Should().Equal(outcome == 0 ? ["save"] : ["save", "assign"]);
        flow.Bus.Verify(events => events.NotifyDeckOpt(), outcome > 0 ? Times.Once() : Times.Never());
        flow.VM.GetPrivateField<DeckOptions>("lastSaved").DailyLimits.Reviews
            .Should().Be(outcome > 0 ? savedReviews + 1 : savedReviews);
        if (outcome == 0)
            flow.Errors.Should().ContainSingle().Which.Should().Contain("save the preset")
                .And.Contain("assignment has not been changed");
        else if (outcome == 1)
            flow.Errors.Should().ContainSingle().Which.Should().Contain("assign the preset")
                .And.Contain("preset itself was saved successfully");
        else
            flow.Errors.Should().BeEmpty();

        if (outcome < 2)
        {
            flow.Dialogs.Verify(dialog => dialog.ShowError(
                It.IsAny<string>(), "Error",
                outcome == 1 ? DialogButtons.RetryCancel : DialogButtons.OK), Times.Once);
        }
    }

    [Fact]
    public async Task AssignmentFailure_DoesNotSwitchToNewlyCreatedPreset()
    {
        var flow = await CreatePresetWorkflow(outcome: 1);
        var current = flow.VM.CurrentOptions;
        current.DailyLimits.Reviews++;
        flow.Service.Setup(service => service.CreateNew(It.IsAny<DeckOptions>()))
            .ReturnsAsync((DeckOptions.CreateNew("New", 7) with { Id = 12 }));
        flow.VM.NewPresetName = "New";

        await flow.VM.AddNewPresetCommand.ExecuteAsync(null);

        flow.VM.CurrentOptions.Should().BeSameAs(current);
        flow.VM.AllPresets.Should().Contain(preset => preset.Id == 12);
        flow.Calls.Should().Equal("save", "assign");
        flow.Errors.Should().ContainSingle();
    }

    [Theory]
    [InlineData(0, false)]
    [InlineData(0, true)]
    [InlineData(1, false)]
    [InlineData(1, true)]
    [InlineData(2, false)]
    [InlineData(2, true)]
    [InlineData(3, false)]
    [InlineData(3, true)]
    [InlineData(4, false)]
    [InlineData(4, true)]
    [InlineData(5, false)]
    [InlineData(5, true)]
    public async Task InvalidPreset_BlocksSaveAndCloseSaveUntilCorrected(int invalidCase, bool fromClosing)
    {
        var flow = await CreatePresetWorkflow(outcome: 2);
        var current = flow.VM.CurrentOptions;
        var lastSaved = flow.VM.GetPrivateField<DeckOptions>("lastSaved");
        MakeInvalid(current, invalidCase);
        var draft = Helpers.GetMapper().Map<DeckOptions>(current);
        bool closed = false;
        flow.VM.OnCloseRequest += () => closed = true;

        if (fromClosing)
            (await flow.VM.CanCloseAsync()).Should().BeFalse();
        else
            await flow.VM.SaveChangesCommand.ExecuteAsync(null);

        closed.Should().BeFalse();
        Helpers.GetMapper().Map<DeckOptions>(current).Should().BeEquivalentTo(draft);
        flow.VM.GetPrivateField<DeckOptions>("lastSaved").Should().BeSameAs(lastSaved);
        flow.Calls.Should().BeEmpty();
        flow.Errors.Should().ContainSingle().Which.Should().Contain("invalid settings");
        flow.Bus.Verify(events => events.NotifyDeckOpt(), Times.Never);

        Helpers.GetMapper().Map(lastSaved, current);
        await flow.VM.SaveChangesCommand.ExecuteAsync(null);

        closed.Should().BeTrue();
        flow.Calls.Should().Equal("save", "assign");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(2)]
    [InlineData(4)]
    [InlineData(5)]
    public async Task InvalidPreset_BlocksSaveBeforeSwitchingPresets(int invalidCase)
    {
        var flow = await CreatePresetWorkflow(outcome: 2);
        var current = flow.VM.CurrentOptions;
        MakeInvalid(current, invalidCase);
        var other = Helpers.GetMapper().Map<DeckOptionsVM>(DeckOptions.CreateNew("Other", 7) with { Id = 12 });

        await flow.VM.ChangePresetCommand.ExecuteAsync(other);

        flow.VM.CurrentOptions.Should().BeSameAs(current);
        flow.Calls.Should().BeEmpty();
        flow.Errors.Should().ContainSingle().Which.Should().Contain("invalid settings");
        other.DeckCount.Should().Be(0);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(2)]
    [InlineData(4)]
    public async Task InvalidPreset_CannotBePersistedByCloning(int invalidCase)
    {
        var flow = await CreatePresetWorkflow(outcome: 2);
        var current = flow.VM.CurrentOptions;
        MakeInvalid(current, invalidCase);
        var draft = Helpers.GetMapper().Map<DeckOptions>(current);

        await flow.VM.ClonePresetCommand.ExecuteAsync(null);

        flow.Service.Verify(service => service.CreateNew(It.IsAny<DeckOptions>()), Times.Never);
        flow.Calls.Should().BeEmpty();
        flow.VM.CurrentOptions.Should().BeSameAs(current);
        flow.VM.AllPresets.Should().ContainSingle().Which.Should().BeSameAs(current);
        Helpers.GetMapper().Map<DeckOptions>(current).Should().BeEquivalentTo(draft);
        flow.Errors.Should().ContainSingle().Which.Should().Contain("invalid settings");
    }

    [Theory]
    [InlineData("save")]
    [InlineData("close")]
    [InlineData("switch")]
    [InlineData("clone")]
    public async Task UnfinishedRename_BlocksPersistenceUntilCancelled(string operation)
    {
        var flow = await CreatePresetWorkflow(outcome: 2);
        var current = flow.VM.CurrentOptions;
        current.DailyLimits.Reviews++;
        current.BeginRenameCommand.Execute(null);
        current.TempName = "Unfinished name";
        bool closed = false;
        flow.VM.OnCloseRequest += () => closed = true;

        switch (operation)
        {
            case "save": await flow.VM.SaveChangesCommand.ExecuteAsync(null); break;
            case "close": (await flow.VM.CanCloseAsync()).Should().BeFalse(); break;
            case "switch":
                var other = Helpers.GetMapper().Map<DeckOptionsVM>(DeckOptions.CreateNew("Other", 7) with { Id = 12 });
                await flow.VM.ChangePresetCommand.ExecuteAsync(other);
                other.DeckCount.Should().Be(0);
                break;
            case "clone": await flow.VM.ClonePresetCommand.ExecuteAsync(null); break;
        }

        flow.Calls.Should().BeEmpty();
        flow.Service.Verify(service => service.CreateNew(It.IsAny<DeckOptions>()), Times.Never);
        flow.Errors.Should().ContainSingle().Which.Should().Contain("being renamed");
        flow.VM.CurrentOptions.Should().BeSameAs(current);
        current.TempName.Should().Be("Unfinished name");
        current.IsRenaming.Should().BeTrue();
        closed.Should().BeFalse();

        current.CancelRenameCommand.Execute(null);
        await flow.VM.SaveChangesCommand.ExecuteAsync(null);
        flow.Calls.Should().Equal("save", "assign");
        closed.Should().BeTrue();
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task InclusiveBounds_CanBeSavedIncludingZeroDailyLimits(bool upperBounds)
    {
        var flow = await CreatePresetWorkflow(outcome: 2);
        var current = flow.VM.CurrentOptions;
        current.Scheduling.GoodMultiplier = current.Scheduling.EasyMultiplier
            = current.Scheduling.HardMultiplier = upperBounds ? 8 : 0.1;
        current.Scheduling.GraduateDayCount = current.Scheduling.EasyOnNewDayCount = upperBounds ? 100 : 1;
        current.DailyLimits.Reviews = current.DailyLimits.Lessons = upperBounds ? 1_000_000 : 0;
        var submitted = Helpers.GetMapper().Map<DeckOptions>(current);

        await flow.VM.SaveChangesCommand.ExecuteAsync(null);

        flow.Service.Verify(service => service.SaveEditedPreset(It.Is<DeckOptions>(preset => preset.Equals(submitted))), Times.Once);
        flow.Calls.Should().Equal("save", "assign");
        flow.Errors.Should().BeEmpty();
    }

    private static void MakeInvalid(DeckOptionsVM preset, int invalidCase)
    {
        switch (invalidCase)
        {
            case 0: preset.Scheduling.GoodMultiplier = 0; break;
            case 1: preset.Scheduling.GraduateDayCount = 101; break;
            case 2: preset.DailyLimits.Reviews = -1; break;
            case 3: preset.DailyLimits.Lessons = 1_000_001; break;
            case 4: preset.Sorting.LessonsOrder = (LessonOrder)99; break;
            case 5: preset.Scheduling.GoodOnNewStage = (LearningStage)99; break;
            default: throw new ArgumentOutOfRangeException(nameof(invalidCase));
        }
    }

    private static async Task<PresetWorkflow> CreatePresetWorkflow(int outcome)
    {
        var mapper = Helpers.GetMapper();
        var preset = DeckOptions.CreateNew("Preset", 7) with { Id = 11 };
        var current = mapper.Map<DeckOptionsVM>(preset);
        current.DeckCount = 1;
        var builder = new Mock<IDeckOptVMBuilder>();
        builder.Setup(service => service.BuildAllCounted(7))
            .ReturnsAsync(new List<DeckOptionsVM> { current });
        var service = new Mock<IDeckOptionsService>(MockBehavior.Strict);
        var calls = new List<string>();
        service.Setup(repo => repo.SaveEditedPreset(It.IsAny<DeckOptions>()))
            .Callback(() => calls.Add("save"))
            .Returns(outcome == 0 ? Task.FromException(new IOException()) : Task.CompletedTask);
        service.Setup(repo => repo.AssignToDeck(1, 11))
            .Callback(() => calls.Add("assign"))
            .Returns(outcome == 1 ? Task.FromException(new IOException()) : Task.CompletedTask);
        var errors = new List<string>();
        var dialogs = new Mock<IDialogService>();
        dialogs.Setup(dialog => dialog.ShowError(It.IsAny<string>(), "Error", It.IsAny<DialogButtons>()))
            .Callback<string, string, DialogButtons>((message, _, _) => errors.Add(message))
            .Returns(DialogResult.Cancel);
        dialogs.Setup(dialog => dialog.ShowWarning(It.IsAny<string>(), It.IsAny<string>(), DialogButtons.YesNoCancel)).Returns(DialogResult.Yes);
        dialogs.Setup(dialog => dialog.ShowWarning(It.IsAny<string>(), It.IsAny<string>(), DialogButtons.YesNoCancel)).Returns(DialogResult.Yes);
        var bus = new Mock<IVMEventBus>();
        var vm = new DeckOptionsMenuVM(mapper, builder.Object, service.Object, Mock.Of<IUserOptionsService>(),
            new Deck { Id = 1, UserId = 7, OptionsId = 11 }, bus.Object,
            NullLogger<DeckOptionsMenuVM>.Instance, dialogs.Object, Helpers.GetSafetyNet(dialogs.Object));
        await vm.InitializeAsync();
        return new(vm, service, bus, calls, errors, dialogs);
    }

    private record PresetWorkflow(DeckOptionsMenuVM VM, Mock<IDeckOptionsService> Service,
        Mock<IVMEventBus> Bus, List<string> Calls, List<string> Errors, Mock<IDialogService> Dialogs);
}