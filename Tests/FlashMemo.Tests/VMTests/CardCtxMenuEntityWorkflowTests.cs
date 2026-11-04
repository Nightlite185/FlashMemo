using System.Collections.ObjectModel;
using FlashMemo.Model.Domain;
using FlashMemo.Model.Persistence;
using FlashMemo.Repositories;
using FlashMemo.Services;
using FlashMemo.Tests.Fakes.Model;
using FlashMemo.ViewModel;
using FlashMemo.ViewModel.Factories;
using FlashMemo.ViewModel.Other;
using FlashMemo.ViewModel.Popups;
using FlashMemo.ViewModel.Windows;
using FlashMemo.ViewModel.Wrappers;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace FlashMemo.Tests.VMTests;

public class CardCtxMenuEntityWorkflowTests
{
    [Fact]
    public async Task OpenMenu_CapturesBaselineBeforeCallerChangesItsEntities()
    {
        var flow = CreateWorkflow();
        var source = CreateCard(1);
        flow.Menu.OpenMenu([source]);
        source.IsSuspended = true;
        ((StandardNote)source.Note).FrontContent = "Changed outside the menu";
        source.Tags.Clear();
        CardEntity? candidate = null;
        flow.Service.Setup(service => service.SaveEditedCards(It.IsAny<IEnumerable<CardEntity>>(), CardAction.Suspend))
            .Callback<IEnumerable<CardEntity>, CardAction>((cards, _) => candidate = cards.Single())
            .Returns(Task.CompletedTask);

        await flow.Menu.ToggleSuspendedCtxCommand.ExecuteAsync(null);

        candidate!.IsSuspended.Should().BeTrue();
        ((StandardNote)candidate.Note).FrontContent.Should().Be("Original front");
        candidate.Tags.Select(tag => tag.Id).Should().Equal(11);
    }

    [Fact]
    public async Task FailedPostpone_CanBeConfirmedAgainWithoutApplyingOffsetTwice()
    {
        var flow = CreateWorkflow();
        var source = CreateCard(1);
        var due = source.Due;
        var attempts = FailFirstSave(flow.Service);
        flow.Menu.OpenMenu([source]);
        flow.Menu.ShowRescheduleCtxCommand.Execute(null);
        var popup = flow.Popup.CurrentPopup.Should().BeOfType<RescheduleVM>().Subject;
        popup.PostponeByDays = 3;
        popup.SinceToday = false;
        popup.KeepInterval = true;

        await popup.ConfirmCommand.ExecuteAsync(null);

        flow.Popup.CurrentPopup.Should().BeSameAs(popup);
        source.Due.Should().Be(due);
        flow.Host.VerifyNoOtherCalls();
        flow.Bus.Verify(bus => bus.NotifyDomain(), Times.Never);

        await popup.ConfirmCommand.ExecuteAsync(null);

        flow.Popup.CurrentPopup.Should().BeNull();
        attempts.Should().HaveCount(2);
        attempts.Should().OnlyContain(cards => cards.Single().Due == due!.Value.AddDays(3));
        attempts[0].Single().Should().NotBeSameAs(attempts[1].Single());
        source.Due.Should().Be(due);
        flow.Host.Verify(host => host.OnActionExecuted(CtxMenuAction.Reschedule,
            It.Is<IReadOnlyCollection<CardEntity>>(cards => cards.Single().Due == due!.Value.AddDays(3))), Times.Once);
        flow.Bus.Verify(bus => bus.NotifyDomain(), Times.Once);
    }

    [Fact]
    public async Task FailedMove_DoesNotLoseEligibleCardsOnNextConfirmation()
    {
        var flow = CreateWorkflow();
        var source = CreateCard(1);
        var alreadyThere = CreateCard(2);
        alreadyThere.MoveToDeck(flow.Target.ToEntity());
        var attempts = FailFirstSave(flow.Service);
        flow.Menu.OpenMenu([source, alreadyThere]);
        await flow.Menu.MoveCardsCtxCommand.ExecuteAsync(null);
        var popup = flow.Popup.CurrentPopup.Should().BeOfType<DeckSelectVM>().Subject;
        popup.SelectedDeck = flow.Target;

        await popup.ConfirmCommand.ExecuteAsync(null);
        flow.Popup.CurrentPopup.Should().BeSameAs(popup);
        source.DeckId.Should().Be(3);
        flow.Host.VerifyNoOtherCalls();

        await popup.ConfirmCommand.ExecuteAsync(null);

        flow.Popup.CurrentPopup.Should().BeNull();
        attempts.Should().HaveCount(2);
        attempts.Should().OnlyContain(cards => cards.Length == 1 && cards[0].Id == 1 && cards[0].DeckId == 99);
        source.DeckId.Should().Be(3);
        flow.Host.Verify(host => host.OnActionExecuted(CtxMenuAction.Relocate,
            It.Is<IReadOnlyCollection<CardEntity>>(cards => cards.Count == 1 && cards.Single().Id == 1)), Times.Once);
    }

    [Fact]
    public async Task DirectToggle_PreservesSourceOnFailureAndUsesCommittedBaselineAfterSuccess()
    {
        var flow = CreateWorkflow();
        var source = CreateCard(1);
        var attempts = FailFirstSave(flow.Service);
        flow.Menu.OpenMenu([source]);

        await flow.Menu.ToggleSuspendedCtxCommand.ExecuteAsync(null);
        source.IsSuspended.Should().BeFalse();
        flow.Host.VerifyNoOtherCalls();
        await flow.Menu.ToggleSuspendedCtxCommand.ExecuteAsync(null);
        await flow.Menu.ToggleSuspendedCtxCommand.ExecuteAsync(null);

        attempts.Select(cards => cards.Single().IsSuspended).Should().Equal(true, true, false);
        source.IsSuspended.Should().BeFalse();
        flow.Host.Verify(host => host.OnActionExecuted(CtxMenuAction.Suspend,
            It.IsAny<IReadOnlyCollection<CardEntity>>()), Times.Exactly(2));
        flow.Bus.Verify(bus => bus.NotifyDomain(), Times.Exactly(2));
    }

    [Fact]
    public async Task FailedDelete_PreservesSelectionForAnotherAttemptAndNotifiesOnlyAfterSuccess()
    {
        var flow = CreateWorkflow();
        flow.Preferences.Setup(service => service.GetFromUser(7))
            .ReturnsAsync(UserOptions.CreateDefault() with { ConfirmCardDeletion = false });
        flow.Repo.SetupSequence(repo => repo.DeleteCards(It.IsAny<IEnumerable<long>>()))
            .ThrowsAsync(new IOException()).Returns(Task.CompletedTask);
        flow.Menu.OpenMenu([CreateCard(1), CreateCard(2)]);

        await flow.Menu.DeleteCardsCtxCommand.ExecuteAsync(null);
        flow.Host.VerifyNoOtherCalls();
        flow.Bus.Verify(bus => bus.NotifyDomain(), Times.Never);
        await flow.Menu.DeleteCardsCtxCommand.ExecuteAsync(null);

        flow.Repo.Verify(repo => repo.DeleteCards(It.Is<IEnumerable<long>>(ids => ids.Order().SequenceEqual(new long[] { 1, 2 }))),
            Times.Exactly(2));
        flow.Host.Verify(host => host.OnActionExecuted(CtxMenuAction.Delete,
            It.Is<IReadOnlyCollection<CardEntity>>(cards => cards.Count == 2)), Times.Once);
        flow.Bus.Verify(bus => bus.NotifyDomain(), Times.Once);
    }

    [Fact]
    public async Task Browse_AppliesSavedDataToOriginalTargetsEvenWhenSelectionChanges()
    {
        var bus = new VMEventBus();
        var browse = new BrowseVM(null!, null!, 7, bus, NullLogger<BrowseVM>.Instance,
            Mock.Of<IDialogService>(), Helpers.GetSafetyNet());
        var first = new CardVM(CreateCard(1)) { IsSelected = true };
        var second = new CardVM(CreateCard(2));
        var cards = browse.GetPrivateField<ObservableCollection<CardVM>>("cards");
        cards.Add(first);
        cards.Add(second);
        var flow = CreateWorkflow(browse, browse, eventBus: bus);
        flow.Service.Setup(service => service.SaveEditedCards(It.IsAny<IEnumerable<CardEntity>>(), CardAction.Suspend))
            .Returns(Task.CompletedTask);
        flow.Menu.OpenMenu([first.SavedEntity]);
        first.IsSelected = false;
        second.IsSelected = true;
        var notifications = new List<string?>();
        first.PropertyChanged += (_, args) => notifications.Add(args.PropertyName);

        await flow.Menu.ToggleSuspendedCtxCommand.ExecuteAsync(null);

        first.IsSuspended.Should().BeTrue();
        second.IsSuspended.Should().BeFalse();
        notifications.Should().Contain(string.Empty);
        first.DeckName.Should().Be("Original deck");
    }

    [Fact]
    public async Task Editor_ContextOperationUsesSavedStateAndPreservesPendingEdits()
    {
        var mapper = Helpers.GetMapper();
        var card = new EditableCardVM(CreateCard(1), new XamlNoteComparer(), mapper);
        ((StandardNoteVM)card.Note).FrontContent = "Unsaved front";
        card.Tags.Clear();
        card.Tags.Add(new TagVM(new Tag { Id = 777, Name = "Unsaved tag", UserId = 7 }));
        card.ChangeDeck(new Deck { Id = 88, UserId = 7, Name = "Unsaved deck" });
        var bus = new VMEventBus();
        var editor = new CardEditorVM(Mock.Of<ICardService>(), Mock.Of<ICardRepo>(), bus,
            null!, card, NullLogger<CardEditorVM>.Instance, Mock.Of<IDialogService>(), Helpers.GetSafetyNet());
        var flow = CreateWorkflow(editor, editor, eventBus: bus);
        editor.Initialize(flow.Menu, Mock.Of<ICardTagsVM>());
        CardEntity? persisted = null;
        flow.Service.Setup(service => service.SaveEditedCards(It.IsAny<IEnumerable<CardEntity>>(), CardAction.Suspend))
            .Callback<IEnumerable<CardEntity>, CardAction>((cards, _) => persisted = cards.Single())
            .Returns(Task.CompletedTask);
        editor.ShowCtxMenuCommand.Execute(null);

        await flow.Menu.ToggleSuspendedCtxCommand.ExecuteAsync(null);

        ((StandardNote)persisted!.Note).FrontContent.Should().Be("Original front");
        persisted.Tags.Select(tag => tag.Id).Should().Equal(11);
        persisted.DeckId.Should().Be(3);
        card.SavedEntity.IsSuspended.Should().BeTrue();
        ((StandardNoteVM)card.Note).FrontContent.Should().Be("Unsaved front");
        card.Tags.Single().Id.Should().Be(777);
        card.Deck.Id.Should().Be(88);
        card.RevertChanges();
        ((StandardNoteVM)card.Note).FrontContent.Should().Be("Original front");
        card.Tags.Single().Id.Should().Be(11);
        card.Deck.Id.Should().Be(3);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Review_AdvancesAndRefreshesCardOnlyAfterSuccessfulContextSave(bool succeeds)
    {
        var (review, flow) = await CreateReviewWorkflow();
        flow.Service.Setup(service => service.SaveEditedCards(It.IsAny<IEnumerable<CardEntity>>(), CardAction.Suspend))
            .Returns(succeeds ? Task.CompletedTask : Task.FromException(new IOException()));
        var original = review.CurrentCard!;
        review.ShowCtxMenuCommand.Execute(null);

        await flow.Menu.ToggleSuspendedCtxCommand.ExecuteAsync(null);

        original.IsSuspended.Should().Be(succeeds);
        original.IsInvalid.Should().Be(succeeds);
        review.CurrentCard!.Id.Should().Be(succeeds ? 2 : 1);
        review.ReviewedCount.Should().Be(succeeds ? 1 : 0);
    }

    [Fact]
    public async Task Review_ContextSaveDoesNotSkipAnUnrelatedCurrentCard()
    {
        var (review, flow) = await CreateReviewWorkflow();
        var completion = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        flow.Service.Setup(service => service.SaveEditedCards(It.IsAny<IEnumerable<CardEntity>>(), CardAction.Suspend))
            .Returns(completion.Task);
        var original = review.CurrentCard!;
        review.ShowCtxMenuCommand.Execute(null);

        var saving = flow.Menu.ToggleSuspendedCtxCommand.ExecuteAsync(null);
        review.CurrentCard = review.activeCards.Pop();
        completion.SetResult();
        await saving;

        original.IsSuspended.Should().BeTrue();
        original.IsInvalid.Should().BeTrue();
        review.CurrentCard.Id.Should().Be(2);
        review.CurrentCard.IsInvalid.Should().BeFalse();
        review.IsSessionFinished.Should().BeFalse();
    }

    private static async Task<(ReviewVM Review, Workflow Flow)> CreateReviewWorkflow()
    {
        var query = new Mock<ICardQueryService>();
        query.Setup(service => service.GetForStudy(3, 7))
            .ReturnsAsync((new List<CardEntity> { CreateCard(1), CreateCard(2) }, new CardsCount { Reviews = 2 }));
        var preferences = new Mock<IUserOptionsService>();
        preferences.Setup(service => service.GetFromUser(7)).ReturnsAsync(UserOptions.CreateDefault());
        var options = new Mock<IDeckOptionsService>();
        options.Setup(service => service.GetFromDeck(3)).ReturnsAsync(DeckOptions.Default);
        var bus = new VMEventBus();
        var review = new ReviewVM(Mock.Of<ICardService>(), query.Object, 7, CreateCard(1).Deck,
            Mock.Of<ICardRepo>(), bus, preferences.Object, options.Object,
            NullLogger<ReviewVM>.Instance, Mock.Of<IDialogService>(), Helpers.GetSafetyNet());
        var flow = CreateWorkflow(review, review, eventBus: bus);
        await review.InitAsync(flow.Menu);
        return (review, flow);
    }

    [Theory]
    [InlineData("study")]
    [InlineData("refresh")]
    [InlineData("review")]
    public async Task ContextSave_FromReviewDataPreservesTagsAndScheduling(string sourceKind)
    {
        using var factory = new FakeDbFactory();
        await new TestsDbSeeder(factory).SeedDefault();
        var original = CreateCard(1);
        await using (var db = factory.CreateDbContext())
        {
            db.Users.Add(new UserEntity { Id = 7, Name = "User", Options = UserOptions.CreateDefault() });
            db.Cards.Add(original);
            await db.SaveChangesAsync();
        }
        var service = new CardService(factory, Helpers.GetMapper());
        var preferences = new UserOptionsService(factory);
        var counter = new Mock<ICountingService>();
        counter.Setup(value => value.CalculateTakings(7, It.IsAny<CardsByStateQ>(), It.IsAny<DeckOptionsEntity>()))
            .ReturnsAsync(new LessonReviewTake(10, 10));
        var query = new CardQueryService(factory, counter.Object, preferences);
        CardEntity source = sourceKind switch
        {
            "study" => (await query.GetForStudy(3, 7)).Item1.Single(),
            "refresh" => (await new CardRepo(factory).GetByIds([1])).Single(),
            "review" => await service.ReviewCardAsync(1, new ScheduleInfo(TimeSpan.FromDays(4), CardState.Review, null),
                Answers.Good, TimeSpan.FromSeconds(3)),
            _ => throw new ArgumentOutOfRangeException(nameof(sourceKind))
        };
        source.Tags.Should().ContainSingle();
        var flow = CreateWorkflow(persistingService: service);
        flow.Menu.OpenMenu([source]);

        await flow.Menu.ToggleSuspendedCtxCommand.ExecuteAsync(null);

        await using var verification = factory.CreateDbContext();
        var saved = await verification.Cards.Include(card => card.Tags).SingleAsync(card => card.Id == 1);
        saved.IsSuspended.Should().BeTrue();
        saved.Tags.Select(tag => tag.Id).Should().Equal(11);
        saved.Interval.Should().Be(source.Interval);
        saved.Due.Should().Be(source.Due);
        saved.State.Should().Be(source.State);
        saved.UserId.Should().Be(source.UserId);
        saved.NoteId.Should().Be(source.NoteId);
        source.IsSuspended.Should().BeFalse();
    }

    private static List<CardEntity[]> FailFirstSave(Mock<ICardService> service)
    {
        var attempts = new List<CardEntity[]>();
        service.Setup(value => value.SaveEditedCards(It.IsAny<IEnumerable<CardEntity>>(), It.IsAny<CardAction>()))
            .Callback<IEnumerable<CardEntity>, CardAction>((cards, _) => attempts.Add(cards.ToArray()))
            .Returns(() => attempts.Count == 1 ? Task.FromException(new IOException()) : Task.CompletedTask);
        return attempts;
    }

    private static Workflow CreateWorkflow(ICtxMenuHost? host = null, IPopupHost? popup = null,
        ICardService? persistingService = null, IVMEventBus? eventBus = null)
    {
        var service = new Mock<ICardService>(MockBehavior.Strict);
        var hostMock = new Mock<ICtxMenuHost>(MockBehavior.Strict);
        hostMock.Setup(value => value.OnActionExecuted(It.IsAny<CtxMenuAction>(), It.IsAny<IReadOnlyCollection<CardEntity>>()))
            .Returns(Task.CompletedTask);
        popup ??= Mock.Of<IPopupHost>();
        var target = new DeckNode(new Deck { Id = 99, UserId = 7, Name = "Target" }, []);
        var builder = new Mock<IDeckTreeBuilder>();
        builder.Setup(value => value.BuildAsync(7)).ReturnsAsync(new List<DeckNode> { target });
        var dialogs = new Mock<IDialogService>();
        var net = Helpers.GetSafetyNet(dialogs.Object);
        var deckFactory = new DeckSelectVMF(builder.Object, NullLogger<DeckSelectVM>.Instance, dialogs.Object, net);
        var repo = new Mock<ICardRepo>(MockBehavior.Strict);
        var preferences = new Mock<IUserOptionsService>(MockBehavior.Strict);
        var bus = new Mock<IVMEventBus>();
        var rescheduleFactory = new RescheduleVMF(NullLogger<RescheduleVM>.Instance, dialogs.Object, net);
        var menuFactory = new CardCtxMenuVMF(repo.Object, persistingService ?? service.Object,
            eventBus ?? bus.Object, deckFactory, preferences.Object, NullLogger<CardCtxMenuVM>.Instance,
            dialogs.Object, net, Helpers.GetMapper(), rescheduleFactory);
        var menu = menuFactory.Create(popup, host ?? hostMock.Object, 7);
        return new(menu, service, hostMock, popup, bus, repo, preferences, dialogs, target);
    }

    private static CardEntity CreateCard(long id) => new()
    {
        Id = id, UserId = 7, DeckId = 3, NoteId = id + 20,
        Deck = new Deck { Id = 3, UserId = 7, Name = "Original deck", OptionsId = DeckOptions.DefaultId },
        Note = new StandardNote { Id = id + 20, FrontContent = "Original front", BackContent = "Original back" },
        Tags = [new Tag { Id = 11, UserId = 7, Name = "Original tag" }],
        State = CardState.Review, Interval = TimeSpan.FromDays(3), Due = DateTime.Today.AddDays(-1),
        Created = DateTime.Today.AddDays(-20), LastReviewed = DateTime.Today.AddDays(-4)
    };

    private record Workflow(CardCtxMenuVM Menu, Mock<ICardService> Service, Mock<ICtxMenuHost> Host,
        IPopupHost Popup, Mock<IVMEventBus> Bus, Mock<ICardRepo> Repo, Mock<IUserOptionsService> Preferences,
        Mock<IDialogService> Dialogs, DeckNode Target);
}