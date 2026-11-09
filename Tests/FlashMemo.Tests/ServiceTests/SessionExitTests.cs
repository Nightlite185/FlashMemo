using FlashMemo.Services;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;

namespace FlashMemo.Tests.ServiceTests;

public class SessionExitTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task ExitSave_WaitsForPersistenceAndOnlyLogsFailures(bool succeeds)
    {
        var completion = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var session = new Mock<ILastSessionService>();
        session.Setup(service => service.SaveStateAsync()).Returns(completion.Task);
        var logger = new Mock<ILogger<MainWindowBootstrapper>>();
        var bootstrapper = new MainWindowBootstrapper(null!, session.Object, logger.Object,
            Mock.Of<IDialogService>());

        var saving = bootstrapper.SaveSessionOnExitAsync();
        saving.IsCompleted.Should().BeFalse();
        var failure = new IOException("Save failed");
        if (succeeds)
            completion.SetResult();
        else
            completion.SetException(failure);
        await saving;

        session.Verify(service => service.SaveStateAsync(), Times.Once);
        logger.Verify(service => service.Log(LogLevel.Error, It.IsAny<EventId>(),
            It.IsAny<It.IsAnyType>(), failure, It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            succeeds ? Times.Never() : Times.Once());
    }
}