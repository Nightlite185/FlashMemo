using System.Windows.Threading;
using FlashMemo.Model.Domain;
using FlashMemo.Services;
using FlashMemo.ViewModel.Other;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace FlashMemo.Tests.VMTests;

public class AnswerRatioVMTests
{
    [Fact]
    public async Task InitFailure_PropagatesWithoutShowingAnInnerErrorDialog()
    {
        var service = new Mock<IStatsQueryService>(MockBehavior.Strict);
        service.Setup(query => query.GetAnswerRatio(Answers.Good, TimePeriod.Month, 7))
            .ThrowsAsync(new IOException());
        var dialogs = new Mock<IDialogService>(MockBehavior.Strict);
        var vm = new AnswerRatioVM(7, service.Object, NullLogger<AnswerRatioVM>.Instance,
            dialogs.Object, Helpers.GetSafetyNet(dialogs.Object));

        Func<Task> initialize = vm.InitAsync;

        await initialize.Should().ThrowAsync<IOException>();
        dialogs.VerifyNoOtherCalls();
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public Task SelectionChange_LoadsCapturedParametersAndUpdatesOnSuccess(bool changeAnswer)
        => Helpers.RunOnSta(async () =>
        {
            var flow = await Workflow.Create();
            var answer = changeAnswer ? Answers.Easy : Answers.Good;
            var period = changeAnswer ? TimePeriod.Month : TimePeriod.Week;
            var pending = new TaskCompletionSource<int>();
            flow.Service.Setup(query => query.GetAnswerRatio(answer, period, 7)).Returns(pending.Task);

            if (changeAnswer) flow.VM.Answer = answer;
            else flow.VM.LastPeriod = period;

            flow.VM.PercentageRatio.Should().Be(40);
            pending.SetResult(0);
            await Flush();

            flow.VM.PercentageRatio.Should().Be(0);
            flow.VM.Answer.Should().Be(answer);
            flow.VM.LastPeriod.Should().Be(period);
            flow.Service.Verify(query => query.GetAnswerRatio(answer, period, 7), Times.Once);
            flow.Service.VerifyNoOtherCalls();
            flow.Dialogs.VerifyNoOtherCalls();
        });

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public Task CancelledFailure_RestoresSelectionWithoutStartingAnotherQuery(bool changeAnswer)
        => Helpers.RunOnSta(async () =>
        {
            var flow = await Workflow.Create();
            var answer = changeAnswer ? Answers.Easy : Answers.Good;
            var period = changeAnswer ? TimePeriod.Month : TimePeriod.Week;
            flow.Service.Setup(query => query.GetAnswerRatio(answer, period, 7)).ThrowsAsync(new IOException());
            flow.CancelErrors();

            if (changeAnswer) flow.VM.Answer = answer;
            else flow.VM.LastPeriod = period;
            await Flush();

            flow.AssertInitialResult();
            flow.Service.Verify(query => query.GetAnswerRatio(answer, period, 7), Times.Once);
            flow.Service.VerifyNoOtherCalls();
            flow.Dialogs.Verify(dialog => dialog.ShowError(It.IsAny<string>(), "Error", DialogButtons.RetryCancel),
                Times.Once);
            flow.Dialogs.VerifyNoOtherCalls();
        });

    [Fact]
    public Task FailedChangeAfterSuccessfulUpdates_RestoresThePreviousSelectionAndPercentage()
        => Helpers.RunOnSta(async () =>
        {
            var flow = await Workflow.Create();
            flow.Service.Setup(query => query.GetAnswerRatio(Answers.Easy, TimePeriod.Month, 7)).ReturnsAsync(55);
            flow.Service.Setup(query => query.GetAnswerRatio(Answers.Easy, TimePeriod.Week, 7)).ReturnsAsync(75);
            flow.VM.Answer = Answers.Easy;
            await Flush();
            flow.VM.LastPeriod = TimePeriod.Week;
            await Flush();

            flow.Service.Setup(query => query.GetAnswerRatio(Answers.Hard, TimePeriod.Week, 7))
                .ThrowsAsync(new IOException());
            flow.CancelErrors();

            flow.VM.Answer = Answers.Hard;
            await Flush();

            flow.VM.Answer.Should().Be(Answers.Easy);
            flow.VM.LastPeriod.Should().Be(TimePeriod.Week);
            flow.VM.PercentageRatio.Should().Be(75);
            flow.Service.Verify(query => query.GetAnswerRatio(Answers.Hard, TimePeriod.Week, 7), Times.Once);
            flow.Dialogs.Verify(dialog => dialog.ShowError(It.IsAny<string>(), "Error", DialogButtons.RetryCancel),
                Times.Once);
        });

    [Fact]
    public Task Retry_LoadsTheSameParametersAndCommitsOnlyTheSuccessfulAttempt()
        => Helpers.RunOnSta(async () =>
        {
            var flow = await Workflow.Create();
            flow.Service.SetupSequence(query => query.GetAnswerRatio(Answers.Easy, TimePeriod.Month, 7))
                .ThrowsAsync(new IOException())
                .ReturnsAsync(65);
            flow.Dialogs.Setup(dialog => dialog.ShowError(It.IsAny<string>(), "Error", DialogButtons.RetryCancel))
                .Callback(() =>
                {
                    flow.VM.PercentageRatio.Should().Be(40);
                })
                .Returns(DialogResult.Retry);

            flow.VM.Answer = Answers.Easy;
            await Flush();

            flow.VM.Answer.Should().Be(Answers.Easy);
            flow.VM.LastPeriod.Should().Be(TimePeriod.Month);
            flow.VM.PercentageRatio.Should().Be(65);
            flow.Service.Verify(query => query.GetAnswerRatio(Answers.Easy, TimePeriod.Month, 7), Times.Exactly(2));
            flow.Service.VerifyNoOtherCalls();
            flow.Dialogs.Verify(dialog => dialog.ShowError(It.IsAny<string>(), "Error", DialogButtons.RetryCancel),
                Times.Once);
        });

    private static async Task Flush()
        => await Dispatcher.Yield(DispatcherPriority.ApplicationIdle);

    private sealed class Workflow
    {
        public Mock<IStatsQueryService> Service { get; } = new(MockBehavior.Strict);
        public Mock<IDialogService> Dialogs { get; } = new(MockBehavior.Strict);
        public AnswerRatioVM VM { get; private set; } = null!;

        public static async Task<Workflow> Create()
        {
            var flow = new Workflow();
            flow.Service.Setup(query => query.GetAnswerRatio(Answers.Good, TimePeriod.Month, 7)).ReturnsAsync(40);
            flow.VM = new AnswerRatioVM(7, flow.Service.Object, NullLogger<AnswerRatioVM>.Instance,
                flow.Dialogs.Object, Helpers.GetSafetyNet(flow.Dialogs.Object));
            await flow.VM.InitAsync();
            flow.Service.Invocations.Clear();
            return flow;
        }

        public void CancelErrors()
            => Dialogs.Setup(dialog => dialog.ShowError(It.IsAny<string>(), "Error", DialogButtons.RetryCancel))
                .Returns(DialogResult.Cancel);

        public void AssertInitialResult()
        {
            VM.Answer.Should().Be(Answers.Good);
            VM.LastPeriod.Should().Be(TimePeriod.Month);
            VM.PercentageRatio.Should().Be(40);
        }
    }
}