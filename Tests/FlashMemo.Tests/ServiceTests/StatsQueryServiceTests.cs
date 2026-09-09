using FlashMemo.Model.Domain;
using FlashMemo.Model.Persistence;
using FlashMemo.Services;
using FlashMemo.Tests.Fakes.Model;
using FlashMemo.ViewModel.Other;
using FluentAssertions;

namespace FlashMemo.Tests.ServiceTests;

public class StatsQueryServiceTests : IDisposable
{
    private const long UserId = 7;
    private readonly FakeDbFactory factory = new();

    [Fact]
    public async Task EmptyHistory_ReturnsNoDataValues()
    {
        var service = new StatsQueryService(factory);

        (await service.DayWithMostReviewsInLastMonth(UserId)).Should().BeNull();
        (await service.AvgAnswerTimeInLastMonth(UserId)).Should().Be(TimeSpan.MinValue);
        (await service.MostReviewedHourOfDayInLastMonth(UserId)).Should().Be(int.MinValue);
        (await service.GetAnswerRatio(Answers.Good, TimePeriod.Month, UserId)).Should().Be(0);
        (await service.TotalReviewsEver(UserId)).Should().Be(0);
        (await service.CurrentReviewStreak(UserId)).Should().Be(0);
        (await service.LongestReviewStreak(UserId)).Should().Be(0);
    }

    [Fact]
    public async Task ReviewHistory_ReturnsExpectedStatistics()
    {
        await SeedReviewHistory();
        var service = new StatsQueryService(factory);

        (await service.DayWithMostReviewsInLastMonth(UserId))
            .Should().Be(DateTime.Today.DayOfWeek);

        (await service.AvgAnswerTimeInLastMonth(UserId))
            .Should().Be(TimeSpan.FromSeconds(25));

        (await service.MostReviewedHourOfDayInLastMonth(UserId)).Should().Be(14);
        (await service.GetAnswerRatio(Answers.Good, TimePeriod.Month, UserId)).Should().Be(75);
        (await service.TotalReviewsEver(UserId)).Should().Be(4);
        (await service.CurrentReviewStreak(UserId)).Should().Be(2);
        (await service.LongestReviewStreak(UserId)).Should().Be(2);
    }

    private async Task SeedReviewHistory()
    {
        var seeder = new TestsDbSeeder(factory);
        seeder.SeedUser();

        await using var db = factory.CreateDbContext();

        db.CardLogs.AddRange(
            ReviewLog(1, DateTime.Today.AddHours(14), Answers.Good, 10),
            ReviewLog(2, DateTime.Today.AddHours(14).AddMinutes(10), Answers.Good, 20),
            ReviewLog(3, DateTime.Today.AddHours(14).AddMinutes(20), Answers.Good, 30),
            ReviewLog(4, DateTime.Today.AddDays(-1).AddHours(9), Answers.Again, 40));

        await db.SaveChangesAsync();
    }

    private static CardLog ReviewLog(
        long id,
        DateTime timestamp,
        Answers answer,
        int answerTimeSeconds)
    {
        return new CardLog
        {
            Id = id,
            UserId = UserId,
            Action = CardAction.Review,
            Answer = answer,
            AnswerTimeSeconds = answerTimeSeconds,
            NewCardState = CardState.Review,
            TimeStamp = timestamp
        };
    }

    public void Dispose() => factory.Dispose();
}
