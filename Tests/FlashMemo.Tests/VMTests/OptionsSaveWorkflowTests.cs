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
        dialogs.Setup(dialog => dialog.Show(It.IsAny<string>(), It.IsAny<string>(),
            DialogButtons.YesNoCancel, DialogIcons.Warning)).Returns(DialogResult.Yes);
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
        dialogs.Setup(dialog => dialog.ShowError(It.IsAny<string>(), "Error", DialogButtons.OK))
            .Callback<string, string, DialogButtons>((message, _, _) => errors.Add(message))
            .Returns(DialogResult.OK);
        dialogs.Setup(dialog => dialog.Show(It.IsAny<string>(), It.IsAny<string>(),
            DialogButtons.YesNoCancel, DialogIcons.Warning)).Returns(DialogResult.Yes);
        dialogs.Setup(dialog => dialog.Show(It.IsAny<string>(), It.IsAny<string>(),
            DialogButtons.YesNoCancel, DialogIcons.Question)).Returns(DialogResult.Yes);
        var bus = new Mock<IVMEventBus>();
        var vm = new DeckOptionsMenuVM(mapper, builder.Object, service.Object, Mock.Of<IUserOptionsService>(),
            new Deck { Id = 1, UserId = 7, OptionsId = 11 }, bus.Object,
            NullLogger<DeckOptionsMenuVM>.Instance, dialogs.Object, Helpers.GetSafetyNet(dialogs.Object));
        await vm.InitializeAsync();
        return new(vm, service, bus, calls, errors);
    }

    private record PresetWorkflow(DeckOptionsMenuVM VM, Mock<IDeckOptionsService> Service,
        Mock<IVMEventBus> Bus, List<string> Calls, List<string> Errors);
}
