using FlashMemo.Model.Domain;
using FlashMemo.Model.Persistence;
using FlashMemo.Services;
using FlashMemo.Tests.Fakes.Model;
using FluentAssertions;

namespace FlashMemo.Tests.ServiceTests;

public class CountingServiceTests: IDisposable
{
    private const long UserId = 7;
    private const long DeckId = 7;
    private readonly FakeDbFactory factory = new();

    [Fact]
    public async Task CalculateTakings_WhenLessonsShareReviewLimitAndReviewsGoFirst_DoesNotExceedActualLessons()
    {
        await SeedUserDeckAndCards(lessonCount: 2, reviewCount: 3);

        var result = await CalculateTakings(
            reviewsCap: 10,
            lessonsCap: 10,
            order: CardStateOrder.ReviewsThenNew);

        result.Should().Be(new LessonReviewTake(Lessons: 2, Reviews: 3));
    }

    [Fact]
    public async Task CalculateTakings_WhenLessonsShareReviewLimitAndLessonsGoFirst_DoesNotExceedActualReviews()
    {
        await SeedUserDeckAndCards(lessonCount: 3, reviewCount: 2);

        var result = await CalculateTakings(
            reviewsCap: 10,
            lessonsCap: 10,
            order: CardStateOrder.NewThenReviews);

        result.Should().Be(new LessonReviewTake(Lessons: 3, Reviews: 2));
    }

    [Fact]
    public async Task CalculateTakings_WhenLessonsGoFirstAndLessonsExceedSharedLimit_LeavesNoReviewSpace()
    {
        await SeedUserDeckAndCards(lessonCount: 10, reviewCount: 10);

        var result = await CalculateTakings(
            reviewsCap: 4,
            lessonsCap: 10,
            order: CardStateOrder.NewThenReviews);

        result.Should().Be(new LessonReviewTake(Lessons: 4, Reviews: 0));
    }

    private async Task<LessonReviewTake> CalculateTakings(
        int reviewsCap,
        int lessonsCap,
        CardStateOrder order)
    {
        var userOptService = new UserOptionsService(factory);
        var counter = new CountingService(factory, null!, userOptService);

        await using var db = factory.CreateDbContext();
        var cardsQ = db.Cards.GroupByStateQ();

        return await counter.CalculateTakings(
            UserId,
            cardsQ,
            DeckOptionsEntityWithLimits(reviewsCap, lessonsCap, order));
    }

    private async Task SeedUserDeckAndCards(int lessonCount, int reviewCount)
    {
        await new TestsDbSeeder(factory).SeedDefault();

        await using var db = factory.CreateDbContext();

        db.Users.Add(new UserEntity()
        {
            Id = UserId,
            Name = "test",
            Options = UserOptions.CreateDefault() with
            {
                IncludeLessonsInReviewLimit = true
            }
        });

        db.Decks.Add(new Deck()
        {
            Id = DeckId,
            UserId = UserId,
            Name = "test",
            OptionsId = DeckOptions.DefaultId
        });

        db.Cards.AddRange(NewCards(lessonCount));
        db.Cards.AddRange(ReviewCards(reviewCount));

        await db.SaveChangesAsync();
    }

    private static DeckOptionsEntity DeckOptionsEntityWithLimits(
        int reviewsCap,
        int lessonsCap,
        CardStateOrder order)
    {
        var options = DefaultDeckOptionsEntity();

        options.DailyLimits.Reviews = reviewsCap;
        options.DailyLimits.Lessons = lessonsCap;
        options.Sorting.CardStateOrder = order;

        return options;
    }

    private static DeckOptionsEntity DefaultDeckOptionsEntity()
    {
        var options = new DeckOptionsEntity()
        {
            Id = DeckOptions.DefaultId,
            Name = DeckOptions.Default.Name
        };

        options.NewOwnedTypes();

        options.DailyLimits.Reviews = DeckOptions.DailyLimitsOpt.DefReviews;
        options.DailyLimits.Lessons = DeckOptions.DailyLimitsOpt.DefLessons;
        options.Sorting.CardStateOrder = DeckOptions.SortingOpt.DefCardStateOrder;

        return options;
    }

    private static IEnumerable<CardEntity> NewCards(int count)
        => Enumerable.Range(1, count)
            .Select(i =>
            {
                var note = new StandardNote()
                {
                    Id = 1_000 + i,
                    FrontContent = $"new {i}",
                    BackContent = ""
                };

                return new CardEntity()
                {
                    Id = i,
                    UserId = UserId,
                    DeckId = DeckId,
                    Note = note,
                    NoteId = note.Id,
                    State = CardState.New,
                    Created = DateTime.Now,
                };
            });

    private static IEnumerable<CardEntity> ReviewCards(int count)
        => Enumerable.Range(1, count)
            .Select(i =>
            {
                var note = new StandardNote()
                {
                    Id = 2_000 + i,
                    FrontContent = $"review {i}",
                    BackContent = ""
                };

                return new CardEntity()
                {
                    Id = 100 + i,
                    UserId = UserId,
                    DeckId = DeckId,
                    Note = note,
                    NoteId = note.Id,
                    State = CardState.Review,
                    Created = DateTime.Now,
                    Due = DateTime.Today.AddDays(-1),
                    Interval = TimeSpan.FromDays(1),
                };
            });

    public void Dispose()
    {
        factory.Dispose();
    }
}
