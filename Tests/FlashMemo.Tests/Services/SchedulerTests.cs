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
    [Fact] public void EasyOnNew()
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
}