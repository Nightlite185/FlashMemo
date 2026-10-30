using System.Windows;
using System.Windows.Threading;
using FlashMemo.Services;
using FlashMemo.ViewModel;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace FlashMemo.Tests.ViewTests;

[Collection("WPF")]
public class WindowClosingTests(WpfTestHost host)
{
    [Theory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    public Task Close_WaitsForDecisionAndDoesNotAskAgainDuringApprovedClose(bool allowsClose, bool asynchronous)
        => host.Run(() =>
        {
            var completion = new TaskCompletionSource<bool>();
            var vm = new ClosingVM { Decision = asynchronous ? completion.Task : Task.FromResult(allowsClose) };
            var window = new Window();
            var service = new WindowService(null!, null!, null!, null!, null!, null!, null!,
                NullLogger<WindowService>.Instance, Mock.Of<IDialogService>());
            service.WireEvents(vm, window);
            bool closed = false;
            window.Closed += (_, _) => closed = true;

            window.Close();
            closed.Should().BeFalse();
            vm.Checks.Should().Be(1);

            if (asynchronous)
            {
                window.Close();
                vm.Checks.Should().Be(1);
                completion.SetResult(allowsClose);
            }

            Dispatcher.CurrentDispatcher.Invoke(() => { }, DispatcherPriority.ApplicationIdle);
            closed.Should().Be(allowsClose);
            vm.Checks.Should().Be(1);

            if (!closed)
            {
                vm.Decision = Task.FromResult(true);
                window.Close();
                Dispatcher.CurrentDispatcher.Invoke(() => { }, DispatcherPriority.ApplicationIdle);
                closed.Should().BeTrue();
            }
        });

    private class ClosingVM : IViewModel, IClosingAware
    {
        public Task<bool> Decision { get; set; } = Task.FromResult(false);
        public int Checks { get; private set; }
        public Task<bool> CanCloseAsync()
        {
            Checks++;
            return Decision;
        }
    }
}
