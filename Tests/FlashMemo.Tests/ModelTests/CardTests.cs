using FlashMemo.Model.Domain;
using FluentAssertions;

namespace FlashMemo.Tests.ModelTests;

public class CardTests
{
    [Fact]
    public void Review_OnNewCard_AppliesScheduleAndReviewTimestamps()
    {
        var card = TestCard.New();
        var schedule = new ScheduleInfo(
            TimeSpan.FromMinutes(8),
            CardState.Learning,
            LearningStage.II);
        var before = DateTime.Now;

        card.Review(schedule);

        var after = DateTime.Now;
        card.State.Should().Be(CardState.Learning);
        card.LearningStage.Should().Be(LearningStage.II);
        card.Interval.Should().Be(TimeSpan.FromMinutes(8));
        card.LastReviewed.Should().BeOnOrAfter(before).And.BeOnOrBefore(after);
        card.Due.Should().BeOnOrAfter(before.Add(schedule.Interval))
            .And.BeOnOrBefore(after.Add(schedule.Interval));
    }

    [Fact]
    public void Review_RejectsNonNewCardWithoutDueDate()
    {
        var card = TestCard.With(
            CardState.Review,
            due: null,
            learningStage: null);

        var act = () => card.Review(ReviewSchedule());

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*Due property is null*");
    }

    [Fact]
    public void Review_RejectsLearningCardWithoutLearningStage()
    {
        var card = TestCard.With(
            CardState.Learning,
            DateTime.Now.AddMinutes(-1),
            learningStage: null);

        var act = () => card.Review(ReviewSchedule());

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*learning state when stage is null*");
    }

    [Fact]
    public void Review_RejectsNonLearningCardWithLearningStage()
    {
        var card = TestCard.With(
            CardState.Review,
            DateTime.Now.AddMinutes(-1),
            LearningStage.I);

        var act = () => card.Review(ReviewSchedule());

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*state other than Learning*");
    }

    [Fact]
    public void Review_RejectsCardDueAfterToday()
    {
        var card = TestCard.With(
            CardState.Review,
            DateTime.Today.AddDays(1),
            learningStage: null);

        var act = () => card.Review(ReviewSchedule());

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*neither due, nor a new card*");
    }

    private static ScheduleInfo ReviewSchedule() => new(
        TimeSpan.FromDays(2),
        CardState.Review,
        LearningStage: null);

    private sealed class TestCard : Card
    {
        public static TestCard New() => new()
        {
            State = CardState.New,
            Due = null,
            LearningStage = null
        };

        public static TestCard With(
            CardState state,
            DateTime? due,
            LearningStage? learningStage) => new()
        {
            State = state,
            Due = due,
            LearningStage = learningStage
        };
    }
}
