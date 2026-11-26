using System.ComponentModel;
using System.Reflection;
using System.Windows;
using FlashMemo.Services;
using FlashMemo.View;
using FlashMemo.ViewModel;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Syncfusion.Licensing;

namespace FlashMemo.Tests.ViewTests;

[Collection("WPF")]
public class FatalShutdownTests(WpfTestHost host) : IDisposable
{
    [Theory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    public Task UnexpectedFailure_LogsAndRequestsShutdownEvenWhenReportingFails(bool logFails, bool dialogFails)
        => host.Run(() =>
        {
            var failure = new InvalidOperationException("Unexpected UI failure");
            bool fatalDuringLog = false;
            bool fatalDuringDialog = false;
            var logger = new Mock<ILogger>();
            logger.Setup(service => service.Log(LogLevel.Critical, It.IsAny<EventId>(),
                    It.IsAny<It.IsAnyType>(), failure, It.IsAny<Func<It.IsAnyType, Exception?, string>>()))
                .Callback(() =>
                {
                    fatalDuringLog = App.IsFatalShutdownStarted;
                    if (logFails) throw new IOException("Logger unavailable");
                });
            var dialogs = new Mock<IDialogService>();
            dialogs.Setup(service => service.ShowError(It.IsAny<string>(), "Fatal error", DialogButtons.OK))
                .Callback(() =>
                {
                    fatalDuringDialog = App.IsFatalShutdownStarted;
                    if (dialogFails) throw new InvalidOperationException("Dialog unavailable");
                })
                .Returns(DialogResult.OK);
            int shutdowns = 0;

            bool handled = App.TryHandleFatalException(failure, logger.Object, dialogs.Object, () => shutdowns++);

            handled.Should().BeTrue();
            fatalDuringLog.Should().BeTrue();
            fatalDuringDialog.Should().BeTrue();
            shutdowns.Should().Be(1);
            logger.Verify(service => service.Log(LogLevel.Critical, It.IsAny<EventId>(),
                It.IsAny<It.IsAnyType>(), failure, It.IsAny<Func<It.IsAnyType, Exception?, string>>()), Times.Once);
            dialogs.Verify(service => service.ShowError(It.IsAny<string>(), "Fatal error", DialogButtons.OK), Times.Once);
        });

    [Fact]
    public Task FailureDuringFatalReporting_DoesNotOpenAnotherDialogOrSuppressDefaultTermination()
        => host.Run(() =>
        {
            bool? nestedHandled = null;
            var dialogs = new Mock<IDialogService>();
            dialogs.Setup(service => service.ShowError(It.IsAny<string>(), "Fatal error", DialogButtons.OK))
                .Callback(() =>
                {
                    nestedHandled = App.TryHandleFatalException(new Exception("Second failure"),
                        NullLogger.Instance, dialogs.Object, () => throw new Exception("Unexpected nested shutdown"));
                })
                .Returns(DialogResult.OK);
            int shutdowns = 0;

            App.TryHandleFatalException(new Exception("Original failure"), NullLogger.Instance,
                dialogs.Object, () => shutdowns++).Should().BeTrue();

            shutdowns.Should().Be(1);
            nestedHandled.Should().BeFalse();
            dialogs.Verify(service => service.ShowError(It.IsAny<string>(), "Fatal error", DialogButtons.OK), Times.Once);
        });

    [Fact]
    public Task FailedShutdown_LeavesExceptionUnhandled()
        => host.Run(() =>
        {
            bool handled = App.TryHandleFatalException(new Exception("UI failure"), NullLogger.Instance,
                Mock.Of<IDialogService>(), () => throw new InvalidOperationException("Shutdown failed"));

            handled.Should().BeFalse();
            App.IsFatalShutdownStarted.Should().BeTrue();
        });

    [Fact]
    public Task FatalShutdown_SkipsDraftClosePromptAndSessionPersistence()
        => host.Run(() =>
        {
            var vm = new Mock<IViewModel>();
            var closing = vm.As<IClosingAware>();
            closing.Setup(service => service.CanCloseAsync()).ReturnsAsync(false);
            var window = new Window();
            var windows = new WindowService(null!, null!, null!, null!, null!, null!, null!,
                Helpers.GetSafetyNet(), NullLogger<WindowService>.Instance);
            windows.WireEvents(vm.Object, window);
            bool closed = false;
            window.Closed += (_, _) => closed = true;
            var session = new Mock<ILastSessionService>();
            var bootstrapper = new MainWindowBootstrapper(null!, session.Object,
                NullLogger<MainWindowBootstrapper>.Instance, Mock.Of<IDialogService>());

            bool handled = App.TryHandleFatalException(new Exception("UI failure"), NullLogger.Instance,
                Mock.Of<IDialogService>(), () =>
                {
                    window.Close();
                    bootstrapper.SaveSessionOnExitAsync().GetAwaiter().GetResult();
                });

            handled.Should().BeTrue();
            closed.Should().BeTrue();
            closing.Verify(service => service.CanCloseAsync(), Times.Never);
            session.Verify(service => service.SaveStateAsync(), Times.Never);
        });

    [Fact]
    public Task FatalShutdown_ClosesCreatorWithoutInspectingUnavailableEditorState()
        => host.Run(() =>
        {
            using (var stream = typeof(App).Assembly.GetManifestResourceStream("FlashMemo.SyncfusionLicense"))
            {
                if (stream is not null)
                {
                    using var reader = new StreamReader(stream);
                    string key = reader.ReadToEnd().Trim();
                    if (!string.IsNullOrWhiteSpace(key)) SyncfusionLicenseProvider.RegisterLicense(key);
                }
            }
            var window = new CreateCardWindow();
            var editor = window.FrontBox;
            bool closed = false;
            window.Closed += (_, _) => closed = true;
            // Fatal closing must not inspect draft controls. A missing reference makes a
            // regression fail before the creator could display a blocking MessageBox.
            window.FrontBox = null!;

            try
            {
                bool handled = App.TryHandleFatalException(new Exception("Unexpected UI failure"), NullLogger.Instance,
                    Mock.Of<IDialogService>(), () => window.Close());

                handled.Should().BeTrue();
                closed.Should().BeTrue();
            }
            finally
            {
                window.FrontBox = editor;
                // Keep cleanup safe even if a regression made the closing handler throw.
                var closing = typeof(CreateCardWindow).GetMethod("OnWindowClosing", BindingFlags.Instance | BindingFlags.NonPublic)!
                    .CreateDelegate<CancelEventHandler>(window);
                window.Closing -= closing;
                window.Close();
            }
        });

    public void Dispose() => App.IsFatalShutdownStarted = false;
}