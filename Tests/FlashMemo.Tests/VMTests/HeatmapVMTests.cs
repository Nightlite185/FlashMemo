using FlashMemo.Model.Persistence;
using FlashMemo.Services;
using FlashMemo.ViewModel.Factories;
using FlashMemo.ViewModel.Other;
using FlashMemo.ViewModel.Windows;
using FlashMemo.ViewModel.Wrappers;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace FlashMemo.Tests.VMTests;

public class HeatmapVMTests
{
    [Theory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    public async Task InitialFailure_KeepsTheVMAliveAndRetriesWithoutResubscribing(bool optionsFail, bool retry)
    {
        var options = new Mock<IUserOptionsService>(MockBehavior.Strict);
        if (optionsFail)
            options.SetupSequence(service => service.GetFromUser(7))
                .ThrowsAsync(new IOException())
                .ReturnsAsync(UserOptions.CreateDefault());
        else
            options.Setup(service => service.GetFromUser(7)).ReturnsAsync(UserOptions.CreateDefault());
        var builder = new Mock<IActivityVMBuilder>(MockBehavior.Strict);
        if (optionsFail)
            builder.Setup(service => service.BuildWeeks(7, It.IsAny<short>())).ReturnsAsync(Weeks());
        else
            builder.SetupSequence(service => service.BuildWeeks(7, It.IsAny<short>()))
                .ThrowsAsync(new IOException())
                .ReturnsAsync(Weeks());
        var bus = new Mock<IVMEventBus>();
        var dialogs = ErrorDialogs(retry);
        var factory = new HeatmapVMF(builder.Object, options.Object, bus.Object,
            NullLogger<HeatmapVM>.Instance, dialogs.Object, Helpers.GetSafetyNet(dialogs.Object));

        var vm = await factory.CreateAsync(7);

        vm.Should().NotBeNull();
        vm.IsLoaded.Should().Be(retry);
        vm.Visibility.Should().Be(retry);
        vm.Weeks.Should().HaveCount(retry ? 1 : 0);
        bus.VerifyAdd(value => value.DomainChanged += It.IsAny<Action>(), Times.Once);
        bus.VerifyAdd(value => value.UserOptionsChanged += It.IsAny<Action>(), Times.Once);
        bus.VerifyRemove(value => value.DomainChanged -= It.IsAny<Action>(), Times.Never);
        bus.VerifyRemove(value => value.UserOptionsChanged -= It.IsAny<Action>(), Times.Never);

        vm.OnFocusLost();
        await vm.OnFocusGained();
        vm.OnFocusLost();
        await vm.OnFocusGained();

        options.Verify(service => service.GetFromUser(7), Times.Exactly(retry ? 2 : 1));
        builder.Verify(service => service.BuildWeeks(7, It.IsAny<short>()),
            Times.Exactly(optionsFail ? (retry ? 1 : 0) : (retry ? 2 : 1)));
        dialogs.Verify(service => service.ShowError(It.IsAny<string>(), "Error", DialogButtons.RetryCancel), Times.Once);
        dialogs.VerifyNoOtherCalls();
        vm.OnClosed();
        bus.VerifyRemove(value => value.DomainChanged -= It.IsAny<Action>(), Times.Once);
        bus.VerifyRemove(value => value.UserOptionsChanged -= It.IsAny<Action>(), Times.Once);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task EnablingHeatmap_LoadsItWithoutReplacingTheDecksOrHeatmapVM(bool initialLoadFails)
    {
        var options = new Mock<IUserOptionsService>(MockBehavior.Strict);
        var currentOptions = UserOptions.CreateDefault() with { ShowHeatmap = initialLoadFails };
        options.Setup(service => service.GetFromUser(7)).ReturnsAsync(() => currentOptions);
        var activity = new Mock<IActivityVMBuilder>(MockBehavior.Strict);
        if (initialLoadFails)
            activity.SetupSequence(service => service.BuildWeeks(7, It.IsAny<short>()))
                .ThrowsAsync(new IOException())
                .ReturnsAsync(Weeks());
        else
            activity.Setup(service => service.BuildWeeks(7, It.IsAny<short>())).ReturnsAsync(Weeks());
        var bus = new VMEventBus();
        var dialogs = ErrorDialogs();
        var net = Helpers.GetSafetyNet(dialogs.Object);
        var heatmapFactory = new HeatmapVMF(activity.Object, options.Object, bus,
            NullLogger<HeatmapVM>.Instance, dialogs.Object, net);
        var tree = new Mock<IDeckTreeBuilder>(MockBehavior.Strict);
        var deck = new DeckNode(new Deck { Id = 1, UserId = 7, Name = "Deck" }, []);
        tree.Setup(service => service.BuildCountedAsync(7)).ReturnsAsync(new[] { deck });
        var factory = new DecksVMF(null!, tree.Object, bus, heatmapFactory, null!, options.Object,
            null!, NullLogger<DecksVM>.Instance, dialogs.Object, null!, net, null!);

        var vm = await factory.CreateAsync(7);
        var originalHeatmap = vm.Heatmap;

        vm.DeckTree.Should().Equal(deck);
        originalHeatmap.Should().NotBeNull();
        originalHeatmap.Visibility.Should().BeFalse();
        if (initialLoadFails)
        {
            currentOptions = currentOptions with { ShowHeatmap = false };
            await ChangeOptions(vm, bus);
        }
        activity.Verify(service => service.BuildWeeks(7, It.IsAny<short>()),
            Times.Exactly(initialLoadFails ? 1 : 0));

        var notifications = new List<string?>();
        originalHeatmap.PropertyChanged += (_, args) => notifications.Add(args.PropertyName);
        currentOptions = currentOptions with { ShowHeatmap = true };
        await ChangeOptions(vm, bus);

        vm.Heatmap.Should().BeSameAs(originalHeatmap);
        originalHeatmap.Visibility.Should().BeTrue();
        originalHeatmap.Weeks.Should().ContainSingle();
        notifications.Should().Contain(nameof(HeatmapVM.Visibility));
        activity.Verify(service => service.BuildWeeks(7, It.IsAny<short>()),
            Times.Exactly(initialLoadFails ? 2 : 1));
        dialogs.Verify(service => service.ShowError(It.IsAny<string>(), "Error", DialogButtons.RetryCancel),
            initialLoadFails ? Times.Once() : Times.Never());
        dialogs.VerifyNoOtherCalls();
        vm.OnClosed();
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task YearFailure_RetriesTheSameYearOrHidesWithoutChangingTheDisplayedYear(bool retry)
    {
        var options = new Mock<IUserOptionsService>(MockBehavior.Strict);
        options.Setup(service => service.GetFromUser(7)).ReturnsAsync(UserOptions.CreateDefault());
        var builder = new Mock<IActivityVMBuilder>(MockBehavior.Strict);
        short currentYear = (short)DateTime.Today.Year;
        short previousYear = (short)(currentYear - 1);
        var originalWeeks = Weeks();
        var newWeeks = Weeks();
        builder.Setup(service => service.BuildWeeks(7, currentYear)).ReturnsAsync(originalWeeks);
        builder.SetupSequence(service => service.BuildWeeks(7, previousYear))
            .ThrowsAsync(new IOException())
            .ReturnsAsync(newWeeks);
        var dialogs = ErrorDialogs(retry);
        var factory = new HeatmapVMF(builder.Object, options.Object, new VMEventBus(),
            NullLogger<HeatmapVM>.Instance, dialogs.Object, Helpers.GetSafetyNet(dialogs.Object));
        var vm = await factory.CreateAsync(7);
        vm.ShiftYearForwardCommand.CanExecute(null).Should().BeFalse();

        await vm.ShiftYearBackCommand.ExecuteAsync(null);

        vm.HeatmapYear.Should().Be(retry ? previousYear : currentYear);
        vm.Visibility.Should().Be(retry);
        vm.Weeks.Should().Equal(retry ? newWeeks : originalWeeks);
        vm.ShiftYearForwardCommand.CanExecute(null).Should().Be(retry);
        vm.ShiftYearBackCommand.CanExecute(null).Should().Be(retry);
        builder.Verify(service => service.BuildWeeks(7, currentYear), Times.Once);
        builder.Verify(service => service.BuildWeeks(7, previousYear), Times.Exactly(retry ? 2 : 1));
        dialogs.Verify(service => service.ShowError(It.IsAny<string>(), "Error", DialogButtons.RetryCancel), Times.Once);
        vm.OnClosed();
    }

    [Fact]
    public async Task DisablingHeatmapWithPendingDomainChanges_DoesNotLoadWeeksUsingTheOldPreference()
    {
        var options = new Mock<IUserOptionsService>(MockBehavior.Strict);
        var currentOptions = UserOptions.CreateDefault();
        options.Setup(service => service.GetFromUser(7)).ReturnsAsync(() => currentOptions);
        var builder = new Mock<IActivityVMBuilder>(MockBehavior.Strict);
        builder.Setup(service => service.BuildWeeks(7, It.IsAny<short>())).ReturnsAsync(Weeks());
        var bus = new VMEventBus();
        var dialogs = new Mock<IDialogService>(MockBehavior.Strict);
        var factory = new HeatmapVMF(builder.Object, options.Object, bus,
            NullLogger<HeatmapVM>.Instance, dialogs.Object, Helpers.GetSafetyNet(dialogs.Object));
        var vm = await factory.CreateAsync(7);

        vm.OnFocusLost();
        currentOptions = currentOptions with { ShowHeatmap = false };
        bus.NotifyDomain();
        bus.NotifyUserOpt();
        await vm.OnFocusGained();

        vm.Visibility.Should().BeFalse();
        vm.Weeks.Should().BeEmpty();
        builder.Verify(service => service.BuildWeeks(7, It.IsAny<short>()), Times.Once);
        dialogs.VerifyNoOtherCalls();
        vm.OnClosed();
    }

    [Fact]
    public async Task ErrorDialogReturningFocus_DoesNotStartAnotherLoad()
    {
        var options = new Mock<IUserOptionsService>(MockBehavior.Strict);
        options.Setup(service => service.GetFromUser(7)).ReturnsAsync(UserOptions.CreateDefault());
        var builder = new Mock<IActivityVMBuilder>(MockBehavior.Strict);
        builder.SetupSequence(service => service.BuildWeeks(7, It.IsAny<short>()))
            .ReturnsAsync(Weeks())
            .ThrowsAsync(new IOException());
        var bus = new VMEventBus();
        var dialogs = new Mock<IDialogService>(MockBehavior.Strict);
        var factory = new HeatmapVMF(builder.Object, options.Object, bus,
            NullLogger<HeatmapVM>.Instance, dialogs.Object, Helpers.GetSafetyNet(dialogs.Object));
        var vm = await factory.CreateAsync(7);
        dialogs.Setup(service => service.ShowError(It.IsAny<string>(), "Error", DialogButtons.RetryCancel))
            .Callback(() =>
            {
                vm.OnFocusLost();
                vm.OnFocusGained().GetAwaiter().GetResult();
            })
            .Returns(DialogResult.Cancel);

        await vm.ReloadDomain(bus);
        vm.OnFocusLost();
        await vm.OnFocusGained();

        vm.Visibility.Should().BeFalse();
        builder.Verify(service => service.BuildWeeks(7, It.IsAny<short>()), Times.Exactly(2));
        dialogs.Verify(service => service.ShowError(It.IsAny<string>(), "Error", DialogButtons.RetryCancel), Times.Once);
        vm.OnClosed();
    }

    [Fact]
    public async Task FailedDecksInitialization_ClosesItsHeatmap()
    {
        var options = new Mock<IUserOptionsService>(MockBehavior.Strict);
        options.Setup(service => service.GetFromUser(7))
            .ReturnsAsync(UserOptions.CreateDefault() with { ShowHeatmap = false });
        var bus = new Mock<IVMEventBus>();
        var dialogs = new Mock<IDialogService>(MockBehavior.Strict);
        var net = Helpers.GetSafetyNet(dialogs.Object);
        var heatmap = new HeatmapVMF(Mock.Of<IActivityVMBuilder>(), options.Object, bus.Object,
            NullLogger<HeatmapVM>.Instance, dialogs.Object, net);
        var tree = new Mock<IDeckTreeBuilder>(MockBehavior.Strict);
        tree.Setup(service => service.BuildCountedAsync(7)).ThrowsAsync(new IOException());
        var factory = new DecksVMF(null!, tree.Object, bus.Object, heatmap, null!, options.Object,
            null!, NullLogger<DecksVM>.Instance, dialogs.Object, null!, net, null!);

        Func<Task> create = () => factory.CreateAsync(7);
        await create.Should().ThrowAsync<IOException>();

        bus.VerifyRemove(value => value.DomainChanged -= It.IsAny<Action>(), Times.Exactly(2));
        bus.VerifyRemove(value => value.UserOptionsChanged -= It.IsAny<Action>(), Times.Exactly(2));
        dialogs.VerifyNoOtherCalls();
    }

    private static async Task ChangeOptions(DecksVM vm, IVMEventBus bus)
    {
        vm.OnFocusLost();
        bus.NotifyUserOpt();
        await vm.OnFocusGained();
    }

    private static ActivityWeekVM[] Weeks() => [new([])];

    private static Mock<IDialogService> ErrorDialogs(bool retry = false)
    {
        var dialogs = new Mock<IDialogService>(MockBehavior.Strict);
        dialogs.Setup(service => service.ShowError(It.IsAny<string>(), "Error", DialogButtons.RetryCancel))
            .Returns(retry ? DialogResult.Retry : DialogResult.Cancel);
        return dialogs;
    }
}