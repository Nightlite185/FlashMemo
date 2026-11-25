using System.ComponentModel;
using System.Reflection;
using System.Windows;
using FlashMemo.Services;
using FlashMemo.View;
using FlashMemo.ViewModel;
using FlashMemo.ViewModel.Bases;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace FlashMemo.Tests.ViewTests;

[Collection("WPF")]
public class WindowCreationTests(WpfTestHost host)
{
    [Fact]
    public Task ResolutionFailure_ReleasesTheSubscribedVMAndReportsTheErrorWithoutRetry() => host.Run(() =>
    {
        var bus = new VMEventBus();
        var vm = new WindowVM(bus);
        var dialogs = ErrorDialogs();
        var service = CreateService(dialogs.Object);
        int resolutions = 0;
        bus.NotifyDomain();

        bool success = TryShow<VMWindow>(service, vm, () =>
        {
            resolutions++;
            throw new IOException("Window resolution failed.");
        });

        success.Should().BeFalse();
        resolutions.Should().Be(1);
        AssertReleased(vm, bus);
        dialogs.VerifyAll();
        dialogs.Invocations.Should().ContainSingle();
    });

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public Task SetupFailure_ReleasesTheVMWhetherClosingSucceedsOrThrows(bool closeFails) => host.Run(() =>
    {
        var bus = new VMEventBus();
        var vm = new WindowVM(bus);
        var dialogs = ErrorDialogs();
        var service = CreateService(dialogs.Object);
        // This window deliberately has no IViewFor<WindowVM>, so setup fails before showing.
        var window = new Window();
        bool closeAttempted = false;
        bool closed = false;
        CancelEventHandler closing = (_, _) =>
        {
            closeAttempted = true;
            if (closeFails) throw new IOException("Window closing failed.");
        };
        window.Closing += closing;
        window.Closed += (_, _) => closed = true;
        bus.NotifyDomain();

        try
        {
            TryShow(service, vm, () => window).Should().BeFalse();

            closeAttempted.Should().BeTrue();
            if (!closeFails) closed.Should().BeTrue();
            AssertReleased(vm, bus);
            dialogs.VerifyAll();
            dialogs.Invocations.Should().ContainSingle();
        }
        finally
        {
            window.Closing -= closing;
            window.Close();
        }
    });

    [Fact]
    public Task ShowFailure_AfterSetup_ReleasesTheVMAndDetachesItsWindowHandlers() => host.Run(() =>
    {
        var bus = new VMEventBus();
        var vm = new WindowVM(bus);
        var dialogs = ErrorDialogs();
        var service = CreateService(dialogs.Object);
        var window = new VMWindow();
        // WPF rejects showing a closed window, so no window is displayed by this test.
        window.Close();
        bus.NotifyDomain();
        dialogs.Setup(dialog => dialog.ShowError(It.IsAny<string>(), "Error", DialogButtons.OK))
            .Callback(() =>
            {
                window.VM.Should().BeSameAs(vm);
                window.DataContext.Should().BeSameAs(vm);
                vm.HasNavigationSubscription.Should().BeTrue();
                vm.HasCloseSubscription.Should().BeTrue();
                vm.IsCleaned.Should().BeFalse();
            })
            .Returns(DialogResult.OK);

        TryShow(service, vm, () => window).Should().BeFalse();

        AssertReleased(vm, bus);
        dialogs.VerifyAll();
        dialogs.Invocations.Should().ContainSingle();
    });

    private static WindowService CreateService(IDialogService dialogs)
        => new(null!, null!, null!, null!, null!, null!, null!,
            Helpers.GetSafetyNet(dialogs), NullLogger<WindowService>.Instance);

    private static Mock<IDialogService> ErrorDialogs()
    {
        var dialogs = new Mock<IDialogService>(MockBehavior.Strict);
        dialogs.Setup(dialog => dialog.ShowError(It.IsAny<string>(), "Error", DialogButtons.OK))
            .Returns(DialogResult.OK);
        return dialogs;
    }

    private static bool TryShow<TWindow>(WindowService service, WindowVM vm, Func<TWindow> getWindow)
        where TWindow : Window
        => (bool)typeof(WindowService)
            .GetMethod("TryWireAndShow", BindingFlags.Instance | BindingFlags.NonPublic)!
            .MakeGenericMethod(typeof(WindowVM), typeof(TWindow))
            .Invoke(service, [vm, getWindow])!;

    private static void AssertReleased(WindowVM vm, VMEventBus bus)
    {
        vm.IsCleaned.Should().BeTrue();
        vm.HasNavigationSubscription.Should().BeFalse();
        vm.HasCloseSubscription.Should().BeFalse();
        bus.NotifyDomain();
        vm.DomainNotifications.Should().Be(1);
    }

    private sealed class VMWindow : Window, IViewFor<WindowVM>
    {
        public WindowVM VM { get; set; } = null!;
    }

    private sealed class WindowVM : IViewModel, IClosedHandler, INavRequestSender, ICloseRequest
    {
        private readonly VMEventBus bus;

        public WindowVM(VMEventBus bus)
        {
            this.bus = bus;
            bus.DomainChanged += OnDomainChanged;
        }

        public bool IsCleaned { get; private set; }
        public int DomainNotifications { get; private set; }
        public bool HasNavigationSubscription => NavRequested is not null;
        public bool HasCloseSubscription => OnCloseRequest is not null;
        public event Func<NavigationRequest, Task>? NavRequested;
        public event Action? OnCloseRequest;
        public void RegisterNavBubbling(INavRequestSender vm) { }

        private void OnDomainChanged() => DomainNotifications++;

        public void OnClosed()
        {
            IsCleaned = true;
            bus.DomainChanged -= OnDomainChanged;
        }
    }
}