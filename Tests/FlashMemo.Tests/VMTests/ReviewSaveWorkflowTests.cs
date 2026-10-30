using System.Diagnostics;
using FlashMemo.Model.Domain;
using FlashMemo.Model.Persistence;
using FlashMemo.Repositories;
using FlashMemo.Services;
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
    public async Task FailedReview_PreservesCardAndForecastAndRestoresTimerPolicy(bool stopsOnReveal)
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
        watch.IsRunning.Should().Be(!stopsOnReveal);
        vm.OnFocusLost();
        await vm.OnFocusGained();
        watch.IsRunning.Should().Be(!stopsOnReveal);
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

    private static async Task<(ReviewVM VM, Mock<ICardService> Service, Mock<IDialogService> Dialogs)>
        CreateWorkflow(bool stopsOnReveal)
    {
        var service = new Mock<ICardService>(MockBehavior.Strict);
        var query = new Mock<ICardQueryService>();
        query.Setup(repo => repo.GetForStudy(3, 7))
            .ReturnsAsync((new List<CardEntity> { CreateCard(1), CreateCard(2) }, new CardsCount { Reviews = 2 }));
        var userOptions = new Mock<IUserOptionsService>();
        userOptions.Setup(repo => repo.GetFromUser(7))
            .ReturnsAsync(UserOptions.CreateDefault() with { TimerStopsOnReveal = stopsOnReveal });
        var deckOptions = new Mock<IDeckOptionsService>();
        deckOptions.Setup(repo => repo.GetFromDeck(3)).ReturnsAsync(DeckOptions.Default);
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
