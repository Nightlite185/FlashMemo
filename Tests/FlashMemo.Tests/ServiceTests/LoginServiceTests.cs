using FlashMemo.Model;
using FlashMemo.Model.Persistence;
using FlashMemo.Services;
using FlashMemo.ViewModel.Factories;
using FlashMemo.ViewModel.Other;
using FlashMemo.ViewModel.Windows;
using FlashMemo.ViewModel.Wrappers;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace FlashMemo.Tests.ServiceTests;

public class LoginServiceTests
{
    [Theory]
    [InlineData(false, false)]
    [InlineData(true, false)]
    [InlineData(false, true)]
    [InlineData(true, true)]
    public async Task PersistenceFailures_OnlyLogAndStillOpenTheCorrectUser(bool saveFails, bool rememberFails)
    {
        using var flow = new Workflow();
        var failure = new IOException("Session persistence failed");
        if (saveFails)
            flow.Session.Setup(service => service.SaveStateAsync()).ThrowsAsync(failure);
        if (rememberFails)
            flow.Session.Setup(service => service.SetLastUserAsync(2)).ThrowsAsync(failure);

        var success = await flow.Login.ChangeUser(2);

        success.Should().BeTrue();
        flow.ActiveCache.UserId.Should().Be(2);
        flow.ActiveCache.LastFilters!.UserId.Should().Be(2);
        flow.ActiveCache.LastUsedDeckId.Should().Be(21);
        flow.ActiveCache.Should().BeSameAs(flow.LoadedCache);
        flow.ActivatedVM.Should().NotBeNull();
        flow.ActivatedVM!.UserId.Should().Be(2);
        flow.Session.Verify(service => service.SaveStateAsync(), Times.Once);
        flow.Session.Verify(service => service.LoadUserCacheAsync(2), Times.Once);
        flow.Cards.Verify(service => service.UnburyIfNextDay(), Times.Once);
        flow.Session.Verify(service => service.SetLastUserAsync(2), Times.Once);
        flow.Boot.Verify(boot => boot.Shutdown(), Times.Never);
        flow.Dialogs.VerifyNoOtherCalls();
        flow.Logger.Verify(logger => logger.Log(LogLevel.Error, It.IsAny<EventId>(),
            It.IsAny<It.IsAnyType>(), failure, It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Exactly((saveFails ? 1 : 0) + (rememberFails ? 1 : 0)));
        flow.Logger.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task SuccessfulSwitch_SavesOldCacheAndActivatesNewCacheBeforeBuildingVMs()
    {
        using var flow = new Workflow();

        await flow.Login.ChangeUser(2);

        flow.Phases.Should().Equal("save", "load cache", "unbury", "heatmap", "decks", "activate main", "remember");
        flow.OriginalCache.UserId.Should().Be(1);
        flow.OriginalCache.LastUsedDeckId.Should().Be(11);
        flow.ActiveCache.Should().BeSameAs(flow.LoadedCache);
    }

    [Fact]
    public async Task Switch_WaitsForOldSessionSaveBeforeChangingTheActiveCache()
    {
        using var flow = new Workflow();
        var completion = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        flow.Session.Setup(service => service.SaveStateAsync()).Returns(completion.Task);

        var switching = flow.Login.ChangeUser(2);

        switching.IsCompleted.Should().BeFalse();
        flow.ActiveCache.Should().BeSameAs(flow.OriginalCache);
        flow.Session.Verify(service => service.LoadUserCacheAsync(It.IsAny<long>()), Times.Never);
        completion.SetResult();
        (await switching).Should().BeTrue();
    }

    [Fact]
    public async Task UnburyFailure_ShowsOneAcknowledgementAndContinuesLogin()
    {
        using var flow = new Workflow();
        flow.Cards.Setup(service => service.UnburyIfNextDay()).ThrowsAsync(new IOException());
        flow.Dialogs.Setup(dialogs => dialogs.ShowError(It.Is<string>(message => message.Contains("remain buried")),
            "Error", DialogButtons.OK)).Returns(DialogResult.OK);

        (await flow.Login.ChangeUser(2)).Should().BeTrue();

        flow.ActivatedVM.Should().NotBeNull();
        flow.Cards.Verify(service => service.UnburyIfNextDay(), Times.Once);
        flow.Dialogs.VerifyAll();
        flow.Dialogs.Invocations.Should().ContainSingle();
        flow.Boot.Verify(boot => boot.Shutdown(), Times.Never);
        flow.Session.Verify(service => service.SetLastUserAsync(2), Times.Once);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task MainCreationOrActivationFailure_ShowsOneFatalDialogAndShutsDown(bool activationFails)
    {
        using var flow = new Workflow();
        var failure = new IOException("Main view failed");
        if (activationFails)
            flow.Boot.Setup(boot => boot.ActivateMainVM(It.IsAny<MainVM>())).Throws(failure);
        else
            flow.Tree.Setup(builder => builder.BuildCountedAsync(2)).ThrowsAsync(failure);
        flow.Dialogs.Setup(dialogs => dialogs.ShowError(It.Is<string>(message => message.Contains("close")),
            "Fatal error", DialogButtons.OK)).Returns(DialogResult.OK);
        flow.Boot.Setup(boot => boot.Shutdown()).Callback(() => flow.Phases.Add("shutdown"));

        (await flow.Login.ChangeUser(2)).Should().BeFalse();

        flow.Dialogs.VerifyAll();
        flow.Dialogs.Invocations.Should().ContainSingle();
        flow.Boot.Verify(boot => boot.ActivateMainVM(It.IsAny<MainVM>()), activationFails ? Times.Once() : Times.Never());
        flow.Boot.Verify(boot => boot.Shutdown(), Times.Once);
        flow.Session.Verify(service => service.SetLastUserAsync(It.IsAny<long>()), Times.Never);
        flow.Subscriptions.Should().BeEmpty();
        flow.Logger.Verify(logger => logger.Log(LogLevel.Critical, It.IsAny<EventId>(),
            It.IsAny<It.IsAnyType>(), failure, It.IsAny<Func<It.IsAnyType, Exception?, string>>()), Times.Once);
        flow.Logger.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task HeatmapFailure_DoesNotBecomeAFatalLoginFailure()
    {
        using var flow = new Workflow();
        flow.Options.Setup(service => service.GetFromUser(2))
            .ReturnsAsync(UserOptions.CreateDefault() with { ShowHeatmap = true });
        flow.Activity.Setup(builder => builder.BuildWeeks(2, It.IsAny<int>())).ThrowsAsync(new IOException());
        flow.Dialogs.Setup(dialogs => dialogs.ShowError(It.IsAny<string>(), "Error", DialogButtons.RetryCancel))
            .Returns(DialogResult.Cancel);

        (await flow.Login.ChangeUser(2)).Should().BeTrue();

        flow.ActivatedVM!.CurrentDisplay.Should().BeOfType<DecksVM>().Which.Heatmap.Visibility.Should().BeFalse();
        flow.Boot.Verify(boot => boot.Shutdown(), Times.Never);
        flow.Dialogs.Verify(dialogs => dialogs.ShowError(It.IsAny<string>(), "Error", DialogButtons.RetryCancel), Times.Once);
        flow.Dialogs.VerifyNoOtherCalls();
        flow.Logger.VerifyNoOtherCalls();
    }

    private sealed class Workflow : IDisposable
    {
        public Mock<ILastSessionService> Session { get; } = new(MockBehavior.Strict);
        public Mock<IMainWindowBootstrapper> Boot { get; } = new(MockBehavior.Strict);
        public Mock<ICardService> Cards { get; } = new(MockBehavior.Strict);
        public Mock<IDialogService> Dialogs { get; } = new(MockBehavior.Strict);
        public Mock<ILogger<LoginService>> Logger { get; } = new();
        public Mock<IDeckTreeBuilder> Tree { get; } = new(MockBehavior.Strict);
        public Mock<IUserOptionsService> Options { get; } = new(MockBehavior.Strict);
        public Mock<IActivityVMBuilder> Activity { get; } = new(MockBehavior.Strict);
        public UserSessionCache OriginalCache { get; } = new() { UserId = 1, LastUsedDeckId = 11, LastFilters = Filters.GetEmpty(1) };
        public UserSessionCache LoadedCache { get; } = new() { UserId = 2, LastUsedDeckId = 21, LastFilters = Filters.GetEmpty(2) };
        public UserSessionCache ActiveCache { get; private set; }
        public MainVM? ActivatedVM { get; private set; }
        public List<string> Phases { get; } = [];
        public HashSet<(string, Action)> Subscriptions { get; } = [];
        public LoginService Login { get; }

        public Workflow()
        {
            ActiveCache = OriginalCache;
            Boot.Setup(boot => boot.EnsureReadyForSwitch()).Returns(true);
            Session.Setup(service => service.SaveStateAsync()).Callback(() =>
            {
                ActiveCache.Should().BeSameAs(OriginalCache);
                Phases.Add("save");
            }).Returns(Task.CompletedTask);
            Session.Setup(service => service.LoadUserCacheAsync(2)).Callback(() =>
            {
                ActiveCache = LoadedCache;
                Phases.Add("load cache");
            }).Returns(Task.CompletedTask);
            Session.SetupGet(service => service.LastFilters).Returns(() => ActiveCache.LastFilters);
            Session.Setup(service => service.SetLastUserAsync(2)).Callback(() =>
            {
                ActivatedVM.Should().NotBeNull();
                Phases.Add("remember");
            }).Returns(Task.CompletedTask);
            Cards.Setup(service => service.UnburyIfNextDay()).Callback(() => Phases.Add("unbury"))
                .Returns(Task.CompletedTask);
            Options.Setup(service => service.GetFromUser(2)).Callback(() =>
            {
                Session.Object.LastFilters!.UserId.Should().Be(2);
                Phases.Add("heatmap");
            }).ReturnsAsync(UserOptions.CreateDefault() with { ShowHeatmap = false });
            Tree.Setup(builder => builder.BuildCountedAsync(2)).Callback(() =>
            {
                Session.Object.LastFilters!.UserId.Should().Be(2);
                Phases.Add("decks");
            }).ReturnsAsync(Array.Empty<DeckNode>());
            var bus = new Mock<IVMEventBus>();
            bus.SetupAdd(value => value.DomainChanged += It.IsAny<Action>())
                .Callback<Action>(handler => Subscriptions.Add(("domain", handler)));
            bus.SetupRemove(value => value.DomainChanged -= It.IsAny<Action>())
                .Callback<Action>(handler => Subscriptions.Remove(("domain", handler)));
            bus.SetupAdd(value => value.UserOptionsChanged += It.IsAny<Action>())
                .Callback<Action>(handler => Subscriptions.Add(("user", handler)));
            bus.SetupRemove(value => value.UserOptionsChanged -= It.IsAny<Action>())
                .Callback<Action>(handler => Subscriptions.Remove(("user", handler)));
            bus.SetupAdd(value => value.DeckOptionsChanged += It.IsAny<Action>())
                .Callback<Action>(handler => Subscriptions.Add(("deck", handler)));
            bus.SetupRemove(value => value.DeckOptionsChanged -= It.IsAny<Action>())
                .Callback<Action>(handler => Subscriptions.Remove(("deck", handler)));
            var net = Helpers.GetSafetyNet(Dialogs.Object);
            var heatmap = new HeatmapVMF(Activity.Object, Options.Object, bus.Object, NullLogger<HeatmapVM>.Instance,
                Dialogs.Object, net);
            var decks = new DecksVMF(null!, Tree.Object, bus.Object, heatmap, Session.Object, Options.Object, null!,
                NullLogger<DecksVM>.Instance, Dialogs.Object, null!, net, null!);
            var main = new MainVMF(new DisplayControlFactory(decks, null!, null!), null!, Session.Object,
                bus.Object, NullLogger<MainVM>.Instance, Dialogs.Object, net);
            Boot.Setup(boot => boot.ActivateMainVM(It.IsAny<MainVM>())).Callback<MainVM>(vm =>
            {
                ActivatedVM = vm;
                Phases.Add("activate main");
            });
            Login = new LoginService(main, Boot.Object, Cards.Object, Session.Object, Dialogs.Object, net, Logger.Object);
        }

        public void Dispose() => ActivatedVM?.OnClosed();
    }
}