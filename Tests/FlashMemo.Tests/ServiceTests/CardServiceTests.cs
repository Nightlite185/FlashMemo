using FlashMemo.Model.Domain;
using FlashMemo.Model.Persistence;
using FlashMemo.Services;
using FlashMemo.Tests.Fakes.Model;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace FlashMemo.Tests.ServiceTests;

public sealed class CardServiceTests : IDisposable
{
    private const long UserId = 71;
    private const long DeckId = 72;
    private const long CardId = 73;
    private const long FirstTagId = 74;
    private const long SecondTagId = 75;

    private readonly FakeDbFactory factory = new();

    [Fact]
    public async Task ReviewCardAsync_PersistsScheduleAndCompleteReviewLog()
    {
        await SeedCardAsync();
        var service = new CardService(factory, Helpers.GetMapper());
        var schedule = new ScheduleInfo(
            TimeSpan.FromDays(4),
            CardState.Review,
            LearningStage: null);
        var before = DateTime.Now;

        await service.ReviewCardAsync(
            CardId,
            schedule,
            Answers.Good,
            TimeSpan.FromMilliseconds(2_900));

        var after = DateTime.Now;
        await using var db = factory.CreateDbContext();
        var card = await db.Cards.SingleAsync(c => c.Id == CardId);
        var log = await db.CardLogs.SingleAsync();

        card.State.Should().Be(CardState.Review);
        card.LearningStage.Should().BeNull();
        card.Interval.Should().Be(schedule.Interval);
        card.LastReviewed.Should().BeOnOrAfter(before).And.BeOnOrBefore(after);
        card.Due.Should().BeOnOrAfter(before.Add(schedule.Interval))
            .And.BeOnOrBefore(after.Add(schedule.Interval));

        log.CardId.Should().Be(CardId);
        log.UserId.Should().Be(UserId);
        log.Action.Should().Be(CardAction.Review);
        log.Answer.Should().Be(Answers.Good);
        log.AnswerTimeSeconds.Should().Be(2);
        log.NewCardState.Should().Be(CardState.Review);
        log.TimeStamp.Should().BeOnOrAfter(before).And.BeOnOrBefore(after);
    }

    [Fact]
    public async Task SaveEditedCard_PersistsScalarsNoteAndTagDiffsAndCreatesLog()
    {
        await SeedCardAsync();
        CardEntity edited;

        await using (var db = factory.CreateDbContext())
        {
            edited = await db.Cards
                .AsNoTracking()
                .Include(c => c.Tags)
                .SingleAsync(c => c.Id == CardId);
        }

        edited.IsSuspended = true;
        edited.LastModified = new DateTime(2026, 9, 15, 12, 30, 0);
        ((StandardNote)edited.Note).FrontContent = "edited front";
        ((StandardNote)edited.Note).BackContent = "edited back";
        edited.Tags = [new Tag { Id = SecondTagId }];

        var service = new CardService(factory, Helpers.GetMapper());
        await service.SaveEditedCard(edited, CardAction.Modify);

        await using var verificationDb = factory.CreateDbContext();
        var saved = await verificationDb.Cards
            .Include(c => c.Tags)
            .SingleAsync(c => c.Id == CardId);
        var log = await verificationDb.CardLogs.SingleAsync();

        saved.IsSuspended.Should().BeTrue();
        saved.LastModified.Should().Be(edited.LastModified);
        ((StandardNote)saved.Note).FrontContent.Should().Be("edited front");
        ((StandardNote)saved.Note).BackContent.Should().Be("edited back");
        saved.Tags.Select(t => t.Id).Should().Equal(SecondTagId);

        log.CardId.Should().Be(CardId);
        log.UserId.Should().Be(UserId);
        log.Action.Should().Be(CardAction.Modify);
        log.Answer.Should().BeNull();
        log.AnswerTimeSeconds.Should().BeNull();
        log.NewCardState.Should().Be(saved.State);
    }

    private async Task SeedCardAsync()
    {
        await new TestsDbSeeder(factory).SeedDefault();

        await using var db = factory.CreateDbContext();
        db.Users.Add(new UserEntity
        {
            Id = UserId,
            Name = "card service user",
            Options = UserOptions.CreateDefault()
        });
        db.Decks.Add(new Deck
        {
            Id = DeckId,
            UserId = UserId,
            Name = "card service deck",
            OptionsId = DeckOptions.DefaultId
        });

        var firstTag = new Tag
        {
            Id = FirstTagId,
            UserId = UserId,
            Name = "first"
        };
        var secondTag = new Tag
        {
            Id = SecondTagId,
            UserId = UserId,
            Name = "second"
        };
        db.Tags.AddRange(firstTag, secondTag);

        var note = new StandardNote
        {
            Id = 76,
            FrontContent = "original front",
            BackContent = "original back"
        };
        db.Cards.Add(new CardEntity
        {
            Id = CardId,
            UserId = UserId,
            DeckId = DeckId,
            NoteId = note.Id,
            Note = note,
            Tags = [firstTag],
            Created = DateTime.Today.AddDays(-10),
            Due = DateTime.Today.AddDays(-1),
            LastReviewed = DateTime.Today.AddDays(-5),
            Interval = TimeSpan.FromDays(3),
            State = CardState.Review
        });

        await db.SaveChangesAsync();
    }

    public void Dispose() => factory.Dispose();
}
