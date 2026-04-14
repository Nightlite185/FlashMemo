using FlashMemo.Model.Domain;
using FlashMemo.Model.Persistence;
using FlashMemo.Repositories;
using FlashMemo.Services;
using FlashMemo.Tests.Fakes.Model;
using FlashMemo.ViewModel.Factories;
using FlashMemo.ViewModel.Windows;
using FlashMemo.ViewModel.Wrappers;
using FluentAssertions;
using Moq.AutoMock;

namespace FlashMemo.Tests.VMTests;

public class ReviewVMTests: IDisposable
{
    #region private things
    private const string stackName = "activeCards";
    private readonly FakeDbFactory factory = new();
    private async Task<ReviewVM> Init()
    {
        const long userId = 7L;

        var seeder = new TestsDbSeeder(factory);
        var mocker = new AutoMocker();

        await seeder.SeedDefault();
        seeder.SeedUser();
        var deck = seeder.SeedDeck();

        SeedCards(deck);

        var mapper = Helpers.GetMapper();
        UserOptionsService userOptS = new(factory);
        DeckOptionsService deckOptS = new(factory, mapper);
        CountingService counter = new(factory, deckOptS, userOptS);

        mocker.Use<IDeckOptionsService>(deckOptS);
        mocker.Use<IUserOptionsService>(userOptS);
        mocker.Use<ICardRepo>(new CardRepo(factory));
        mocker.Use<ICardService>(new CardService(factory, mapper));
        mocker.Use<ICardQueryService>(new CardQueryService(factory, counter, userOptS));

        var vmf = mocker.CreateInstance<ReviewVMF>();
        return await vmf.CreateAsync(userId, deck);
    }
    
    ///<summary>Seeds one of each state; 1 review, 1 new, and 1 learning.</summary>
    private void SeedCards(IDeckMeta deck)
    {
        var db = factory.CreateDbContext();
        var note = new StandardNote()
            {Id = 7, FrontContent = "", BackContent = ""};

        var newCard = CardEntity.CreateNew(note, deck, []);
        newCard.Id = 1;
        
        var reviewCard = new CardEntity()
        {
            State = CardState.Review,
            Due = DateTime.Today.AddDays(-1),
            DeckId = 7,
            UserId = 7,
            Id = 2,
            Interval = TimeSpan.FromDays(2),
            NoteId = note.Id,
        };

        var learningCard = new CardEntity()
        {
            State = CardState.Learning,
            Due = DateTime.Now.AddMinutes(-5),
            DeckId = 7,
            UserId = 7,
            Id = 3,
            LearningStage = LearningStage.I,
            Interval = DeckOptions.SchedulingOpt.DefLearningStages.I,
            NoteId = note.Id,
        };

        db.Cards.AddRange(
            newCard, 
            reviewCard, 
            learningCard);

        db.SaveChanges();
    }
    
    private static async Task AnswerGood(ReviewVM vm)
    {
        vm.RevealAnswerCommand.Execute(null);
        await vm.GoodAnswerCommand.ExecuteAsync(null);
    }
    private static async Task AnswerEasy(ReviewVM vm)
    {
        vm.RevealAnswerCommand.Execute(null);
        await vm.EasyAnswerCommand.ExecuteAsync(null);
    }
    #endregion

    [Fact] public async Task LoadsCardsInCorrectOrderWithCorrectCount()
    {
        var vm = await Init();

        var DbCards = factory.CreateDbContext()
            .Cards.ToDictionary(c => c.State);

        var stack = vm.GetPrivateField<Stack<CardVM>>(stackName);

        var card1 = vm.CurrentCard?.ToEntity();
        var card2 = stack.Pop().ToEntity();
        var card3 = stack.Pop().ToEntity();

        // according to card-by-state order from default deckOptions,
        // cards in vm's stack should be in following order:
        // 1. Learning, 2. Review, 3. Lessons

        // we are checking order and deep equality in the same time.
        card1.Should().BeEquivalentTo(DbCards[CardState.Learning]);
        card2.Should().BeEquivalentTo(DbCards[CardState.Review]);
        card3.Should().BeEquivalentTo(DbCards[CardState.New]);

        vm.CardsCount.Should().BeEquivalentTo(new CardsCount()
            {Learning = 1, Lessons  = 1, Reviews = 1}, 
            opt => opt.ComparingByMembers<CardsCount>());
    }

    [Fact] public async Task ReviewsCardAndSavesCorrectly()
    {
        var vm = await Init();
        var card = vm.CurrentCard;

        card.Should().NotBeNull();

        var expectedSchedule = Scheduler.GetSchedule(
            card, DeckOptions.SchedulingOpt.Default, 
            UserOptions.CreateDefault(), Answers.Good);

        var expectedCard = new CardEntity()
        {
            State = expectedSchedule.State,
            LearningStage = expectedSchedule.LearningStage,
            Interval = expectedSchedule.Interval,
        };

        vm.RevealAnswerCommand.Execute(null);
        await vm.GoodAnswerCommand.ExecuteAsync(null);

        var db = factory.CreateDbContext();

        var result = db.Cards.Where(c =>
            c.State == CardState.Learning)
            .Single();

        result.Should().BeEquivalentTo(expectedCard, opt => opt
            .Including(c => c.State)
            .Including(c => c.LearningStage)
            .Including(c => c.Interval));

        result.LastReviewed.Should().NotBeNull();
        result.LastReviewed.Value.Date.Should().Be(DateTime.Today);
    }

    [Fact] public async Task KeepsCorrectCountThroughoutSession()
    {
        static void compareCount(ReviewVM vm, CardsCount expected)
        {
            vm.CardsCount.Should().BeEquivalentTo(expected, opt => 
            opt.ComparingByMembers<CardsCount>());
        }

        var vm = await Init();

        // we have 1 learning, 1 review, 1 lesson in this exact order.

        var count1 = new CardsCount()
        { Learning = 1, Lessons = 1, Reviews = 1};

        var count2 = new CardsCount()
        { Learning = 1, Lessons = 1, Reviews = 0};

        var count3 = new CardsCount()
        { Learning = 1, Lessons = 0, Reviews = 0};

        var emptyCount4 = new CardsCount()
        { Learning = 0, Lessons = 0, Reviews = 0};

        vm.InitialCount.Should().Be(3);

        // good on learning I -> goes to learning pool (count should include it).
        await AnswerGood(vm);
        compareCount(vm, count1);
        vm.ReviewedCount.Should().Be(0);

        // good on review -> out of the session.
        await AnswerGood(vm);
        compareCount(vm, count2);
        vm.ReviewedCount.Should().Be(1);

        // easy on new -> out of the session.
        await AnswerEasy(vm);
        compareCount(vm, count3);
        vm.ReviewedCount.Should().Be(2);

        // now only card left is the one from learning pool,
        // that should be popped early bc its the last one.
        await AnswerEasy(vm);
        compareCount(vm, emptyCount4);
        vm.ReviewedCount.Should().Be(3);

        vm.IsSessionFinished.Should().BeTrue();
    }

    public void Dispose()
    {
        factory.Dispose();
    }
}