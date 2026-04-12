using System.Linq.Expressions;
using FlashMemo.Helpers;
using FlashMemo.Model.Domain;
using FlashMemo.Model.Persistence;
using FlashMemo.Tests.Fakes.Model;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace FlashMemo.Tests.ServiceTests;

public class ExpressionTranslationTests : IDisposable
{
    private readonly FakeDbFactory factory = new();

    [Fact]
    public async Task MixedFiltersQuery_ShouldTranslateAndReturnMatchingCard()
    {
        var data = await SeedCardsAsync();
        using var db = factory.CreateDbContext();

        var query = BuildQuery(data.UserId, data.DeckId, data.TagId, data.Day);

        var result = await db.Cards
            .Where(query)
            .ToListAsync();

        result.Should().ContainSingle();
        result[0].Id.Should().Be(data.MatchingCardId);
    }

    private async Task<SeedData> SeedCardsAsync()
    {
        var seeder = new TestsDbSeeder(factory);
        await seeder.SeedDefault();
        var user = seeder.SeedUser();
        var deck = seeder.SeedDeck();

        using var db = factory.CreateDbContext();

        const long matchingCardId = 2001;
        const long nonMatchingCardId = 2002;
        const long tagId = 3001;

        var tag = new Tag
        {
            Id = tagId,
            UserId = user.Id,
            Name = "tag",
            IntColor = 0
        };

        var day = new DateTime(2026, 04, 13, 0, 0, 0, DateTimeKind.Local);

        db.Tags.Add(tag);
        db.Cards.AddRange(
            new CardEntity
            {
                Id = matchingCardId,
                UserId = user.Id,
                DeckId = deck.Id,
                Note = new StandardNote
                {
                    Id = 1001,
                    FrontContent = "front",
                    BackContent = "back"
                },
                Tags = [tag],
                IsBuried = false,
                IsSuspended = false,
                Created = day.AddHours(9),
                Due = day.AddHours(11),
                LastReviewed = day.AddHours(12),
                LastModified = day.AddHours(13),
                Interval = TimeSpan.FromDays(5),
                State = CardState.Review
            },
            new CardEntity
            {
                Id = nonMatchingCardId,
                UserId = user.Id,
                DeckId = deck.Id,
                Note = new StandardNote
                {
                    Id = 1002,
                    FrontContent = "other",
                    BackContent = "other"
                },
                Tags = [],
                IsBuried = false,
                IsSuspended = true,
                Created = day.AddDays(-1),
                Due = day.AddDays(-1),
                LastReviewed = day.AddDays(-1),
                LastModified = day.AddDays(-1),
                Interval = TimeSpan.FromDays(2),
                State = CardState.New
            });

        await db.SaveChangesAsync();

        return new SeedData(user.Id, deck.Id, tagId, matchingCardId, day);
    }

    private static Expression<Func<CardEntity, bool>> BuildQuery(
        long userId,
        long deckId,
        long tagId,
        DateTime day)
    {
        var dayStart = day.Date;
        var dayEnd = dayStart.AddDays(1);

        Expression<Func<CardEntity, bool>> query = c => c.UserId == userId;

        query = query.Combine(c => !c.IsBuried);
        query = query.Combine(c => !c.IsSuspended);
        query = query.Combine(c => c.DeckId == deckId);
        query = query.Combine(c => c.State == CardState.Review);
        query = query.Combine(c => c.Created >= dayStart && c.Created < dayEnd);
        query = query.Combine(c => c.Due >= dayStart && c.Due.Value < dayEnd);
        query = query.Combine(c => c.LastReviewed >= dayStart && c.LastReviewed.Value < dayEnd);
        query = query.Combine(c => c.LastModified >= dayStart && c.LastModified.Value < dayEnd);
        query = query.Combine(c => c.Tags.Select(t => t.Id).Contains(tagId));

        return query;
    }

    public void Dispose()
    {
        factory.Dispose();
    }

    private readonly record struct SeedData(
        long UserId,
        long DeckId,
        long TagId,
        long MatchingCardId,
        DateTime Day);
}
