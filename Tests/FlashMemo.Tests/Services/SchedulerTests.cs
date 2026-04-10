using FlashMemo.Helpers;
using FlashMemo.Model.Domain;
using FlashMemo.Model.Persistence;
using FlashMemo.Services;
using FlashMemo.Tests.Fakes.Model;

namespace FlashMemo.Tests.Services;

public class SchedulerTests
{
    private static (UserOptions userOpt, DeckOptions.SchedulingOpt deckOpt) DefaultOpt()
    {
        return (UserOptions.CreateDefault(), 
                DeckOptions.Default.Scheduling);
    }
    private static FakeScheduleInfoCard NewCard => new()
    {
        State = CardState.New,
        Interval = TimeSpan.Zero,
        LearningStage = null,
        DaysOverdue = null
    };
    private static FakeScheduleInfoCard LearningCard(
    DeckOptions.SchedulingOpt opt, LearningStage ls) => new()
    {
        State = CardState.Learning,
        Interval = opt.LearningStages[ls],
        LearningStage = ls,
        DaysOverdue = null
    };
 
    private static FakeScheduleInfoCard RandomReviewCard(bool overdue = false, Random? r = null)
    {
        r ??= Random.Shared;

        return new()
        {
            Interval = TimeSpan.FromDays(
                r.NextDouble(low: 1, high: 365)),

            State = CardState.Review,
            DaysOverdue = overdue ? r.Next(1, 365) : null,
            LearningStage = null
        };
    }
    private static IEnumerable<FakeScheduleInfoCard> RandomReviewCards(bool overdue = false)
    {
        var rand = new Random();

        for (int i = 0; i < 15; i++)
            yield return RandomReviewCard(overdue, rand);
    }

    #region happy path on new cards
    [Fact] public void GoodOnNew()
    {
        (var userOpt, var deckOpt) = DefaultOpt();
        var card = NewCard;

        const Answers answer = Answers.Good;

        var expected = new ScheduleInfo()
        {
            State = CardState.Learning,
            Interval = deckOpt.LearningStages[deckOpt.GoodOnNewStage],
            LearningStage = deckOpt.GoodOnNewStage,
        };
        var result = Scheduler.GetSchedule(
            card, deckOpt, 
            userOpt, answer);

        Assert.Equal(expected, result);
    }
    [Fact] public void EasyOnNew_Graduate()
    {
        (var userOpt, var deckOpt) = DefaultOpt();
        var card = NewCard;

        const Answers answer = Answers.Easy;

        var expected = new ScheduleInfo()
        {
            State = CardState.Review,
            Interval = TimeSpan.FromDays(deckOpt.EasyOnNewDayCount),
            LearningStage = null,
        };
        var result = Scheduler.GetSchedule(
            card, deckOpt,
            userOpt, answer);

        Assert.Equal(expected, result);
    }
    [Fact] public void HardOnNew()
    {
        (var userOpt, var deckOpt) = DefaultOpt();
        var card = NewCard;

        const Answers answer = Answers.Hard;

        var expected = new ScheduleInfo()
        {
            State = CardState.Learning,
            Interval = deckOpt.LearningStages[deckOpt.HardOnNewStage],
            LearningStage = deckOpt.HardOnNewStage,
        };
        var result = Scheduler.GetSchedule(
            card, deckOpt,
            userOpt, answer);

        Assert.Equal(expected, result);
    }
    [Fact] public void AgainOnNew()
    {
        (var userOpt, var deckOpt) = DefaultOpt();
        var card = NewCard;

        const Answers answer = Answers.Again;

        var expected = new ScheduleInfo()
        {
            State = CardState.Learning,
            Interval = deckOpt.LearningStages[LearningStage.I],
            LearningStage = LearningStage.I,
        };
        var result = Scheduler.GetSchedule(
            card, deckOpt,
            userOpt, answer);

        Assert.Equal(expected, result);
    }
    #endregion

    #region happy path on learning cards
    [Fact] public void GoodOn1stLearning_AscendByOne()
    {
        (var userOpt, var deckOpt) = DefaultOpt();
        var card = LearningCard(deckOpt, LearningStage.I);

        const Answers answer = Answers.Good;

        var expected = new ScheduleInfo()
        {
            State = CardState.Learning,
            Interval = deckOpt.LearningStages[LearningStage.II],
            LearningStage = LearningStage.II
        };
        var result = Scheduler.GetSchedule(
            card, deckOpt,
            userOpt, answer);

        Assert.Equal(0, result.Interval.Days);
        Assert.Equal(expected, result);
    }
    [Fact] public void GoodOnLastLearning_Graduate()
    {
        (var userOpt, var deckOpt) = DefaultOpt();
        var card = LearningCard(deckOpt, LearningStage.III);

        const Answers answer = Answers.Good;

        var expected = new ScheduleInfo()
        {
            State = CardState.Review,
            Interval = TimeSpan.FromDays(deckOpt.GraduateDayCount),
            LearningStage = null
        };
        var result = Scheduler.GetSchedule(
            card, deckOpt,
            userOpt, answer);

        Assert.True(result.Interval.Days > 0);
        Assert.Equal(expected, result);
    }
    [Fact] public void EasyOn1stLearning_Graduate()
    {
        (var userOpt, var deckOpt) = DefaultOpt();
        var card = LearningCard(deckOpt, LearningStage.I);

        const Answers answer = Answers.Easy;

        var expected = new ScheduleInfo()
        {
            State = CardState.Review,
            Interval = TimeSpan.FromDays(deckOpt.GraduateDayCount),
            LearningStage = null
        };
        var result = Scheduler.GetSchedule(
            card, deckOpt,
            userOpt, answer);

        Assert.True(result.Interval.Days > 0);
        Assert.Equal(expected, result);
    }
    [Fact] public void HardOn2ndLearning_NoChanges()
    {
        (var userOpt, var deckOpt) = DefaultOpt();
        var card = LearningCard(deckOpt, LearningStage.II);

        const Answers answer = Answers.Hard;

        var expected = new ScheduleInfo()
        {
            State = CardState.Learning,
            Interval = deckOpt.LearningStages[card.LearningStage],
            LearningStage = card.LearningStage
        };
        var result = Scheduler.GetSchedule(
            card, deckOpt,
            userOpt, answer);

        Assert.Equal(0, result.Interval.Days);
        Assert.Equal(expected, result);

        Assert.True( // checking if all 3 (tested, result, options) intervals are same
            card.Interval == result.Interval
            && result.Interval == deckOpt.LearningStages[card.LearningStage]);
    }
    [Fact] public void AgainOnLastLearning_BackTo1stStage()
    {
        (var userOpt, var deckOpt) = DefaultOpt();
        var card = LearningCard(deckOpt, LearningStage.III);

        const Answers answer = Answers.Again;

        var expected = new ScheduleInfo()
        {
            State = CardState.Learning,
            Interval = deckOpt.LearningStages[LearningStage.I],
            LearningStage = LearningStage.I
        };
        var result = Scheduler.GetSchedule(
            card, deckOpt,
            userOpt, answer);

        Assert.Equal(0, result.Interval.Days);
        Assert.Equal(expected, result);
    }
    #endregion

    #region happy path on review cards
    [Fact] public void GoodOnReview()
    {
        (var userOpt, var deckOpt) = DefaultOpt();
        var cards = RandomReviewCards();

        const Answers answer = Answers.Good;

        foreach(var card in cards)
        {
            var expected = new ScheduleInfo()
            {
                State = CardState.Review,
                Interval = card.Interval * deckOpt.GoodMultiplier,
                LearningStage = null
            };
            var result = Scheduler.GetSchedule(
                card, deckOpt,
                userOpt, answer);

            Assert.True(result.Interval.Days > 0 
                && result.Interval > card.Interval);

            Assert.Equal(expected, result);
        }
    }
    [Fact] public void EasyOnReview()
    {
        (var userOpt, var deckOpt) = DefaultOpt();
        var cards = RandomReviewCards();

        const Answers answer = Answers.Easy;

        foreach(var card in cards)
        {
            var expected = new ScheduleInfo()
            {
                State = CardState.Review,
                Interval = card.Interval * deckOpt.EasyMultiplier,
                LearningStage = null
            };
            var result = Scheduler.GetSchedule(
                card, deckOpt,
                userOpt, answer);

            Assert.True(result.Interval.Days > 0 
                && result.Interval > card.Interval);

            Assert.Equal(expected, result);
        }
    }
    [Fact] public void HardOnReview()
    {
        (var userOpt, var deckOpt) = DefaultOpt();
        var cards = RandomReviewCards();

        const Answers answer = Answers.Hard;

        foreach(var card in cards)
        {
            var expected = new ScheduleInfo()
            {
                State = CardState.Review,
                Interval = card.Interval * deckOpt.HardMultiplier,
                LearningStage = null
            };
            var result = Scheduler.GetSchedule(
                card, deckOpt,
                userOpt, answer);

            Assert.True(result.Interval.Days > 0);
            Assert.Equal(expected, result);
        }
    }
    [Fact] public void AgainOnReview()
    {
        (var userOpt, var deckOpt) = DefaultOpt();
        var cards = RandomReviewCards();

        const Answers answer = Answers.Again;

        foreach(var card in cards)
        {
            var expected = new ScheduleInfo()
            {
                State = CardState.Learning,
                Interval = deckOpt.LearningStages[deckOpt.AgainOnReviewStage],
                LearningStage = deckOpt.AgainOnReviewStage
            };
            var result = Scheduler.GetSchedule(
                card, deckOpt,
                userOpt, answer);

            Assert.Equal(0, result.Interval.Days);
            Assert.Equal(expected, result);
        }
    }
    [Fact] public void GoodOnReview_WithOverdueScaling()
    {
        (var userOpt, var deckOpt) = DefaultOpt();
        var cards = RandomReviewCards(overdue: true);

        const Answers answer = Answers.Good;

        foreach(var card in cards)
        {
            var interval = card.Interval;

            if (card.DaysOverdue is int daysOverdue and > 0
            && userOpt.IntervalScalingOnOverdueness)
            {
                interval += (TimeSpan.FromDays(daysOverdue) / 2);
            }

            var expected = new ScheduleInfo()
            {
                State = CardState.Review,
                Interval = interval * deckOpt.GoodMultiplier,
                LearningStage = null
            };
            var result = Scheduler.GetSchedule(
                card, deckOpt,
                userOpt, answer);

            Assert.True(result.Interval.Days > 0
            && result.Interval > card.Interval);

            Assert.Equal(expected, result);
        }
    }
    [Fact] public void GoodOnReview_OverdueButOptedOut()
    {
        (var userOpt, var deckOpt) = DefaultOpt();
        userOpt.IntervalScalingOnOverdueness = false;

        var cards = RandomReviewCards(overdue: true);

        const Answers answer = Answers.Good;

        foreach(var card in cards)
        {
            var expected = new ScheduleInfo()
            {
                State = CardState.Review,
                Interval = card.Interval * deckOpt.GoodMultiplier,
                LearningStage = null
            };
            var result = Scheduler.GetSchedule(
                card, deckOpt,
                userOpt, answer);

            Assert.True(result.Interval.Days > 0
            && result.Interval > card.Interval);

            Assert.Equal(expected, result);
        }
    }

    [Fact] public void GoodOnReview_WantOverdueButIs0() // edge case, should be either null or > 0
    {
        (var userOpt, var deckOpt) = DefaultOpt();
        var cards = RandomReviewCards(overdue: true);

        const Answers answer = Answers.Good;

        foreach(var card in cards)
        {
            card.DaysOverdue = 0;

            var expected = new ScheduleInfo()
            {
                State = CardState.Review,
                Interval = card.Interval * deckOpt.GoodMultiplier,
                LearningStage = null
            };
            var result = Scheduler.GetSchedule(
                card, deckOpt,
                userOpt, answer);

            Assert.True(result.Interval.Days > 0
            && result.Interval > card.Interval);

            Assert.Equal(expected, result);
        }
    }
    #endregion
}