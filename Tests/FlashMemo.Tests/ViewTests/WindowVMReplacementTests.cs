using System.Reflection;
using System.Windows;
using FlashMemo.Services;
using FlashMemo.View;
using FlashMemo.ViewModel;
using FlashMemo.ViewModel.Bases;
using FlashMemo.ViewModel.Windows;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace FlashMemo.Tests.ViewTests;

[Collection("WPF")]
public class WindowVMReplacementTests(WpfTestHost host)
{
    [Fact]
    public Task ReplacingDataContext_UsesOnlyTheCurrentVMAndReleasesTheOldSubscriptions() => host.Run(() =>
    {
        var service = new WindowService(null!, null!, null!, null!, null!, null!, null!,
            NullLogger<WindowService>.Instance);
        var first = new WindowVM();
        var current = new WindowVM();
        var window = new Window();
        service.WireEvents(first, window);
        bool closed = false;
        window.Closed += (_, _) => closed = true;

        RaiseWindowEvent(window, "OnActivated");
        window.DataContext = current;
        RaiseWindowEvent(window, "OnActivated");
        RaiseWindowEvent(window, "OnDeactivated");

        first.Gained.Should().Be(1);
        first.Lost.Should().Be(0);
        first.Cleanups.Should().Be(1);
        first.HasNavigationSubscription.Should().BeFalse();
        current.Gained.Should().Be(1);
        current.Lost.Should().Be(1);
        current.HasNavigationSubscription.Should().BeTrue();

        first.RequestClose();
        closed.Should().BeFalse();
        current.RequestClose();
        closed.Should().BeTrue();
        current.Cleanups.Should().Be(1);
        current.HasNavigationSubscription.Should().BeFalse();
        current.RequestClose(); // The closed window is no longer retained by this event.
    });

    [Fact]
    public Task SwitchingUsers_WithAnotherWindowOpen_WarnsWithoutSavingOrClosingIt() => host.Run(() =>
    {
        var other = new Window();
        bool closed = false;
        other.Closed += (_, _) => closed = true;
        var session = new Mock<ILastSessionService>(MockBehavior.Strict);
        var cards = new Mock<ICardService>(MockBehavior.Strict);
        var dialogs = new Mock<IDialogService>();
        var bootstrapper = new MainWindowBootstrapper(null!, session.Object,
            NullLogger<MainWindowBootstrapper>.Instance, dialogs.Object);
        var login = new LoginService(null!, bootstrapper, cards.Object, session.Object, dialogs.Object);

        try
        {
            login.ChangeUser(7).GetAwaiter().GetResult().Should().BeFalse();

            closed.Should().BeFalse();
            dialogs.Verify(service => service.Show("Close other windows", It.IsAny<string>(),
                DialogButtons.OK, DialogIcons.Warning), Times.Once);
            session.VerifyNoOtherCalls();
            cards.VerifyNoOtherCalls();
        }
        finally { other.Close(); }
    });

    [Theory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    public Task SwitchingUsers_WithOnlyMainAndChooserWindows_IsAllowed(bool hasMain, bool hasChooser)
        => host.Run(() =>
    {
        var main = hasMain ? new MainWindow() : null;
        var chooser = hasChooser ? new UserSelectWindow() : null;
        var dialogs = new Mock<IDialogService>(MockBehavior.Strict);
        var bootstrapper = new MainWindowBootstrapper(null!, null!,
            NullLogger<MainWindowBootstrapper>.Instance, dialogs.Object);

        try
        {
            bootstrapper.EnsureReadyForSwitch().Should().BeTrue();
            dialogs.VerifyNoOtherCalls();
        }
        finally
        {
            chooser?.Close();
            main?.Close();
        }
    });

    [Fact]
    public Task ActivatingMainVM_ReusesTheWindowAndReleasesThePreviousVM() => host.Run(() =>
    {
        var window = new MainWindow();
        var service = new WindowService(null!, null!, null!, null!, null!, null!, null!,
            NullLogger<WindowService>.Instance);
        var previous = new MainVM(null!, null!, Mock.Of<IVMEventBus>(), 1,
            NullLogger<MainVM>.Instance, null!, null!);
        var current = new MainVM(null!, null!, Mock.Of<IVMEventBus>(), 2,
            NullLogger<MainVM>.Instance, null!, null!);
        var previousDisplay = new Mock<IClosedHandler>();
        var currentDisplay = new Mock<IClosedHandler>();
        previous.CurrentDisplay = previousDisplay.Object;
        current.CurrentDisplay = currentDisplay.Object;
        window.ChangeDataCtx(previous);
        service.WireEvents(previous, window);
        var bootstrapper = new MainWindowBootstrapper(null!, null!,
            NullLogger<MainWindowBootstrapper>.Instance, Mock.Of<IDialogService>());

        try
        {
            bootstrapper.ActivateMainVM(current);

            Application.Current.Windows.OfType<MainWindow>().Should().ContainSingle()
                .Which.Should().BeSameAs(window);
            window.DataContext.Should().BeSameAs(current);
            window.VM.Should().BeSameAs(current);
            previousDisplay.Verify(display => display.OnClosed(), Times.Once);
            currentDisplay.Verify(display => display.OnClosed(), Times.Never);
        }
        finally { window.Close(); }

        previousDisplay.Verify(display => display.OnClosed(), Times.Once);
        currentDisplay.Verify(display => display.OnClosed(), Times.Once);
    });

    private static void RaiseWindowEvent(Window window, string method)
        => typeof(Window).GetMethod(method, BindingFlags.Instance | BindingFlags.NonPublic)!
            .Invoke(window, [EventArgs.Empty]);

    private sealed class WindowVM : IViewModel, IFocusState, IClosedHandler, INavRequestSender, ICloseRequest
    {
        public int Gained { get; private set; }
        public int Lost { get; private set; }
        public int Cleanups { get; private set; }
        public bool HasNavigationSubscription => NavRequested is not null;
        public event Func<NavigationRequest, Task>? NavRequested;
        public event Action? OnCloseRequest;
        public void RegisterNavBubbling(INavRequestSender vm) { }
        public Task OnFocusGained() { Gained++; return Task.CompletedTask; }
        public void OnFocusLost() => Lost++;
        public void OnClosed() => Cleanups++;
        public void RequestClose() => OnCloseRequest?.Invoke();
    }
}