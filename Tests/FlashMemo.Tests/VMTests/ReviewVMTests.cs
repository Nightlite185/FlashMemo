using FlashMemo.Model.Domain;
using FlashMemo.Model.Persistence;
using FlashMemo.Repositories;
using FlashMemo.Services;
using FlashMemo.Tests.Fakes.Model;
using FlashMemo.ViewModel.Factories;
using FlashMemo.ViewModel.Windows;
using FluentAssertions;
using Moq.AutoMock;

namespace FlashMemo.Tests.VMTests;

public class ReviewVMTests: IDisposable
{
    private async Task<ReviewVM> Init()
    {
        const long userId = 7L;

        var seeder = new TestsDbSeeder(factory);
        var mocker = new AutoMocker();

        await seeder.SeedDefault();
        seeder.SeedUser();
        var deck = seeder.SeedDeck();

        SeedCards(deck);

        var userOptService = new UserOptionsService(factory);
        var mapper = Helpers.GetMapper();
        
        mocker.Use<ICardService>(new CardService(factory, mapper));
        mocker.Use<ICardQueryService>(new CardQueryService(
            factory, new CountingService(
                factory, new DeckOptionsService(
                    factory, mapper),
                userOptService), 
            userOptService));
        
        mocker.Use<IUserOptionsService>(new UserOptionsService(factory));
        mocker.Use<ICardRepo>(new CardRepo(factory));

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
    private readonly FakeDbFactory factory = new();

    [Fact] public async Task LoadsCardsInCorrectOrder()
    {
        var vm = await Init();

        var cards = factory.CreateDbContext()
            .Cards.ToDictionary(c => c.State);

        var vmCards = vm.Cards
            .Select(c => c.ToEntity())
            .Reverse() // reversing bc Stack<> has opposite order of what we mean in this test
            .ToArray();

        // according to card-by-state order from default deckOptions,
        // cards in vm's stack should be in following order:
        // 1. Learning, 2. Review, 3. Lessons

        // we are checking order and deep equality in the same time.
        vmCards[0].Should().BeEquivalentTo(cards[CardState.Learning]);
        vmCards[1].Should().BeEquivalentTo(cards[CardState.Review]);
        vmCards[2].Should().BeEquivalentTo(cards[CardState.New]);
    }

    public void Dispose()
    {
        factory.Dispose();
    }
}