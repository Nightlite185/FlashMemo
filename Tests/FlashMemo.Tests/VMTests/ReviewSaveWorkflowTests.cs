using System.Diagnostics;
using System.Reflection;
using FlashMemo.Model.Domain;
using FlashMemo.Model.Persistence;
using FlashMemo.Repositories;
using FlashMemo.Services;
using FlashMemo.ViewModel.Other;
using FlashMemo.ViewModel.Windows;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace FlashMemo.Tests.VMTests;

public class ReviewSaveWorkflowTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task FailedReview_PreservesCardAndForecastAndKeepsOriginalAnswerTime(bool stopsOnReveal)
    {
        var (vm, service, dialogs) = await CreateWorkflow(stopsOnReveal);
        var watch = vm.GetPrivateField<Stopwatch>("stopWatch");
        vm.RevealAnswerCommand.Execute(null);
        var current = vm.CurrentCard;
        var forecast = vm.SchedulePerms;
        service.Setup(repo => repo.ReviewCardAsync(1, It.IsAny<ScheduleInfo>(), Answers.Good, It.IsAny<TimeSpan>()))
            .ThrowsAsync(new IOException());
        dialogs.Setup(dialog => dialog.ShowError(It.IsAny<string>(), "Error", DialogButtons.OK))
            .Callback(() => watch.IsRunning.Should().BeFalse())
            .Returns(DialogResult.OK);

        await vm.GoodAnswerCommand.ExecuteAsync(null);

        vm.CurrentCard.Should().BeSameAs(current);
        vm.SchedulePerms.Should().BeSameAs(forecast);
        vm.AnswerRevealed.Should().BeTrue();
        vm.ReviewedCount.Should().Be(0);
        vm.ReviewHistory.Should().BeEmpty();
        vm.activeCards.Should().ContainSingle();
        vm.GoodAnswerCommand.CanExecute(null).Should().BeTrue();
        watch.IsRunning.Should().BeFalse();
        var originalAnswerTime = watch.Elapsed;
        vm.OnFocusLost();
        await vm.OnFocusGained();
        watch.IsRunning.Should().BeFalse();
        watch.Elapsed.Should().Be(originalAnswerTime);

        service.Setup(repo => repo.ReviewCardAsync(1, It.IsAny<ScheduleInfo>(), Answers.Good, originalAnswerTime))
            .ReturnsAsync(CreateCard(1));
        await vm.GoodAnswerCommand.ExecuteAsync(null);

        service.Verify(repo => repo.ReviewCardAsync(1, It.IsAny<ScheduleInfo>(), Answers.Good, originalAnswerTime), Times.Exactly(2));
        vm.CurrentCard!.Id.Should().Be(2);
        watch.IsRunning.Should().BeTrue();
    }

    [Fact]
    public async Task SuccessfulReview_ExcludesSaveTimeAndResetsTimerBeforeShowingNextCard()
    {
        var (vm, service, _) = await CreateWorkflow(stopsOnReveal: true);
        var watch = vm.GetPrivateField<Stopwatch>("stopWatch");
        vm.RevealAnswerCommand.Execute(null);
        var answerTime = watch.Elapsed;
        answerTime.Should().BeGreaterThan(TimeSpan.Zero);
        var completion = new TaskCompletionSource<CardEntity>(TaskCreationOptions.RunContinuationsAsynchronously);
        service.Setup(repo => repo.ReviewCardAsync(1, It.IsAny<ScheduleInfo>(), Answers.Good, answerTime))
            .Returns(completion.Task);
        TimeSpan? elapsedWhenNextCardAppeared = null;
        vm.PropertyChanged += (_, args) =>
        {
            if (args.PropertyName == nameof(vm.CurrentCard) && vm.CurrentCard?.Id == 2)
                elapsedWhenNextCardAppeared = watch.Elapsed;
        };

        var saving = vm.GoodAnswerCommand.ExecuteAsync(null);
        vm.IsSavingReview.Should().BeTrue();
        vm.HardAnswerCommand.CanExecute(null).Should().BeFalse();
        vm.OnFocusLost();
        await vm.OnFocusGained();
        watch.IsRunning.Should().BeFalse();
        watch.Elapsed.Should().Be(answerTime);
        await vm.HardAnswerCommand.ExecuteAsync(null);
        service.Verify(repo => repo.ReviewCardAsync(It.IsAny<long>(), It.IsAny<ScheduleInfo>(),
            It.IsAny<Answers>(), It.IsAny<TimeSpan>()), Times.Once);
        completion.SetResult(CreateCard(1));
        await saving;

        vm.CurrentCard!.Id.Should().Be(2);
        elapsedWhenNextCardAppeared.Should().Be(TimeSpan.Zero);
        vm.IsSavingReview.Should().BeFalse();
        vm.ReviewedCount.Should().Be(1);
        vm.AnswerRevealed.Should().BeFalse();
        watch.IsRunning.Should().BeTrue();
    }

    [Theory]
    [InlineData(false, "undefined stage")]
    [InlineData(true, "undefined stage")]
    [InlineData(false, "day-count overflow")]
    [InlineData(true, "day-count overflow")]
    [InlineData(false, "interval overflow")]
    [InlineData(true, "interval overflow")]
    public async Task FailedForecast_PreservesCardAndTimerAndAllowsAnotherReveal(bool stopsOnReveal, string failure)
    {
        var scheduling = failure switch
        {
            "undefined stage" => DeckOptions.SchedulingOpt.Default with { AgainOnReviewStage = (LearningStage)99 },
            "day-count overflow" => DeckOptions.SchedulingOpt.Default with { EasyOnNewDayCount = int.MaxValue },
            _ => DeckOptions.SchedulingOpt.Default
        };
        var invalid = DeckOptions.Default with
        {
            Scheduling = scheduling
        };
        var (vm, service, dialogs) = await CreateWorkflow(stopsOnReveal, invalid);
        var current = vm.CurrentCard;
        if (failure != "undefined stage")
        {
            var card = CreateCard(1);
            if (failure == "day-count overflow")
                card.State = CardState.New;
            else
                card.Interval = TimeSpan.MaxValue;
            current!.Refresh(card);
        }
        var watch = vm.GetPrivateField<Stopwatch>("stopWatch");
        dialogs.Setup(dialog => dialog.ShowError(It.IsAny<string>(), "Error", DialogButtons.OK))
            .Callback(() =>
            {
                vm.IsPreparingForecast.Should().BeTrue();
                vm.RevealAnswerCommand.CanExecute(null).Should().BeFalse();
                vm.GoodAnswerCommand.CanExecute(null).Should().BeFalse();
                vm.OnFocusLost();
                vm.OnFocusGained().GetAwaiter().GetResult();
                watch.IsRunning.Should().BeFalse();
            })
            .Returns(DialogResult.OK);

        await vm.RevealAnswerCommand.ExecuteAsync(null);
        await vm.GoodAnswerCommand.ExecuteAsync(null);

        vm.CurrentCard.Should().BeSameAs(current);
        vm.AnswerRevealed.Should().BeFalse();
        vm.SchedulePerms.Should().BeNull();
        vm.IsPreparingForecast.Should().BeFalse();
        vm.RevealAnswerCommand.CanExecute(null).Should().BeTrue();
        watch.IsRunning.Should().BeTrue();
        vm.activeCards.Should().ContainSingle();
        service.VerifyNoOtherCalls();
        dialogs.Verify(dialog => dialog.ShowError(It.IsAny<string>(), "Error", DialogButtons.OK), Times.Once);

        typeof(ReviewVM).GetField("deckOptions", BindingFlags.Instance | BindingFlags.NonPublic)!
            .SetValue(vm, DeckOptions.Default);
        current!.Refresh(CreateCard(1));
        await vm.RevealAnswerCommand.ExecuteAsync(null);

        vm.AnswerRevealed.Should().BeTrue();
        vm.SchedulePerms.Should().NotBeNull();
        vm.GoodAnswerCommand.CanExecute(null).Should().BeTrue();
        watch.IsRunning.Should().Be(!stopsOnReveal);
    }

    [Fact]
    public async Task MissingForecast_DisablesSubmissionWithoutRegeneratingOrSaving()
    {
        var (vm, service, dialogs) = await CreateWorkflow(stopsOnReveal: true);
        await vm.RevealAnswerCommand.ExecuteAsync(null);
        vm.SchedulePerms = null;

        vm.GoodAnswerCommand.CanExecute(null).Should().BeFalse();
        await vm.GoodAnswerCommand.ExecuteAsync(null);

        vm.CurrentCard!.Id.Should().Be(1);
        vm.SchedulePerms.Should().BeNull();
        service.VerifyNoOtherCalls();
        dialogs.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task InvalidAnswer_IsReportedWithoutSavingOrDiscardingTheForecast()
    {
        var (vm, service, dialogs) = await CreateWorkflow(stopsOnReveal: true);
        await vm.RevealAnswerCommand.ExecuteAsync(null);
        var forecast = vm.SchedulePerms;

        var reviewing = (Task)typeof(ReviewVM)
            .GetMethod("ReviewAsync", BindingFlags.Instance | BindingFlags.NonPublic)!
            .Invoke(vm, [(Answers)99])!;
        await reviewing;

        vm.CurrentCard!.Id.Should().Be(1);
        vm.AnswerRevealed.Should().BeTrue();
        vm.SchedulePerms.Should().BeSameAs(forecast);
        vm.IsSavingReview.Should().BeFalse();
        service.VerifyNoOtherCalls();
        dialogs.Verify(dialog => dialog.ShowError(It.IsAny<string>(), "Error", DialogButtons.OK), Times.Once);
    }

    [Theory]
    [InlineData(CardState.New)]
    [InlineData((CardState)99)]
    public async Task InvalidSavedState_EndsSessionAndNeverRetriesTheSavedReview(CardState savedState)
    {
        var (vm, service, dialogs) = await CreateWorkflow(stopsOnReveal: false);
        await vm.RevealAnswerCommand.ExecuteAsync(null);
        var saved = CreateCard(1);
        saved.State = savedState;
        service.Setup(repo => repo.ReviewCardAsync(1, It.IsAny<ScheduleInfo>(), Answers.Good, It.IsAny<TimeSpan>()))
            .ReturnsAsync(saved);

        await vm.GoodAnswerCommand.ExecuteAsync(null);
        await vm.GoodAnswerCommand.ExecuteAsync(null);
        vm.OnFocusLost();
        await vm.OnFocusGained();

        vm.IsSessionFinished.Should().BeTrue();
        vm.CurrentCard.Should().BeNull();
        vm.AnswerRevealed.Should().BeFalse();
        vm.SchedulePerms.Should().BeNull();
        vm.RevealAnswerCommand.CanExecute(null).Should().BeFalse();
        vm.GoodAnswerCommand.CanExecute(null).Should().BeFalse();
        vm.GetPrivateField<Stopwatch>("stopWatch").IsRunning.Should().BeFalse();
        service.Verify(repo => repo.ReviewCardAsync(1, It.IsAny<ScheduleInfo>(), Answers.Good, It.IsAny<TimeSpan>()), Times.Once);
        dialogs.Verify(dialog => dialog.ShowError(
            It.Is<string>(message => message.Contains("Your answer was saved")), "Error", DialogButtons.OK), Times.Once);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task MissingCard_MenuAndEditorReportFailureWithoutAdvancing(bool contextMenu)
    {
        var (vm, service, dialogs) = await CreateWorkflow(stopsOnReveal: true);
        vm.CurrentCard = null;
        var queued = vm.activeCards.Peek();

        if (contextMenu)
            vm.ShowCtxMenuCommand.Execute(null);
        else
            await vm.ShowCardEditCommand.ExecuteAsync(null);

        vm.CurrentCard.Should().BeNull();
        vm.activeCards.Should().ContainSingle().Which.Should().BeSameAs(queued);
        service.VerifyNoOtherCalls();
        dialogs.Verify(dialog => dialog.ShowError(
            It.Is<string>(message => message.Contains("no card is currently loaded")), "Error", DialogButtons.OK), Times.Once);
    }

    [Fact]
    public async Task MissingCard_OnRevealAdvancesWithFreshStateAndTimer()
    {
        var (vm, _, _) = await CreateWorkflow(stopsOnReveal: true);
        await vm.RevealAnswerCommand.ExecuteAsync(null);
        vm.CurrentCard = null;

        await vm.RevealAnswerCommand.ExecuteAsync(null);

        vm.CurrentCard!.Id.Should().Be(2);
        vm.AnswerRevealed.Should().BeFalse();
        vm.SchedulePerms.Should().BeNull();
        vm.ElapsedTime.Should().Be("00:00");
        vm.GetPrivateField<Stopwatch>("stopWatch").IsRunning.Should().BeTrue();
        vm.RevealAnswerCommand.CanExecute(null).Should().BeTrue();
        vm.GoodAnswerCommand.CanExecute(null).Should().BeFalse();
    }

    [Fact]
    public async Task SkippingRevealedCard_ClearsItsForecastAndStartsTheNextCardTimer()
    {
        var (vm, _, _) = await CreateWorkflow(stopsOnReveal: true);
        await vm.RevealAnswerCommand.ExecuteAsync(null);

        await vm.OnActionExecuted(CtxMenuAction.Suspend, [CreateCard(1)]);

        vm.CurrentCard!.Id.Should().Be(2);
        vm.AnswerRevealed.Should().BeFalse();
        vm.SchedulePerms.Should().BeNull();
        vm.GetPrivateField<Stopwatch>("stopWatch").IsRunning.Should().BeTrue();
        vm.GoodAnswerCommand.CanExecute(null).Should().BeFalse();
        vm.RevealAnswerCommand.CanExecute(null).Should().BeTrue();
    }

    private static async Task<(ReviewVM VM, Mock<ICardService> Service, Mock<IDialogService> Dialogs)>
        CreateWorkflow(bool stopsOnReveal, DeckOptions? preset = null)
    {
        var service = new Mock<ICardService>(MockBehavior.Strict);
        var query = new Mock<ICardQueryService>();
        query.Setup(repo => repo.GetForStudy(3, 7))
            .ReturnsAsync((new List<CardEntity> { CreateCard(1), CreateCard(2) }, new CardsCount { Reviews = 2 }));
        var userOptions = new Mock<IUserOptionsService>();
        userOptions.Setup(repo => repo.GetFromUser(7))
            .ReturnsAsync(UserOptions.CreateDefault() with { TimerStopsOnReveal = stopsOnReveal });
        var deckOptions = new Mock<IDeckOptionsService>();
        deckOptions.Setup(repo => repo.GetFromDeck(3)).ReturnsAsync(preset ?? DeckOptions.Default);
        var dialogs = new Mock<IDialogService>();
        var vm = new ReviewVM(service.Object, query.Object, 7,
            new Deck { Id = 3, UserId = 7, Name = "Deck" }, Mock.Of<ICardRepo>(), new VMEventBus(),
            userOptions.Object, deckOptions.Object, NullLogger<ReviewVM>.Instance, dialogs.Object,
            Helpers.GetSafetyNet(dialogs.Object));
        await vm.InitAsync(null!);
        return (vm, service, dialogs);
    }

    private static CardEntity CreateCard(long id) => new()
    {
        Id = id,
        UserId = 7,
        Note = new StandardNote { FrontContent = "Front", BackContent = "Back" },
        Deck = new Deck { Id = 3, UserId = 7, Name = "Deck" },
        DeckId = 3,
        State = CardState.Review,
        Interval = TimeSpan.FromDays(2),
        Due = DateTime.Today.AddDays(-1)
    };
}