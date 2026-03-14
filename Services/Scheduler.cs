using FlashMemo.Model;
using FlashMemo.Model.Domain;

namespace FlashMemo.Services;
   
public static class Scheduler
{
    public static SchedulePermutations GetForecast(IScheduleInfoCard card, DeckOptions.SchedulingOpt options)
    {
        return new(
            Easy: ProcessEasy(card, options),
            Good: ProcessGood(card, options),
            Hard: ProcessHard(card, options),
            Again: ProcessAgain(card, options)
        );
    }
    
    #region Private answer handlers
    private static ScheduleInfo ProcessHard(IScheduleInfoCard card, DeckOptions.SchedulingOpt s)
    {
        var multiplied = card.Interval * s.HardMultiplier;
        var oneDay = TimeSpan.FromDays(1);

        return card.State switch
        {
            CardState.New => new(
                State: CardState.Learning,
                LearningStage: s.HardOnNewStage,
                Interval: s.LearningStages[s.HardOnNewStage]
            ),

            CardState.Learning => new(
                LearningStage: card.LearningStage,
                Interval: s.LearningStages[card.LearningStage],
                State: card.State
            ),

            CardState.Review => new(
                Interval: (multiplied > oneDay)
                    ? multiplied
                    : oneDay,

                State: card.State,
                LearningStage: null
            ),

            _ => throw new ArgumentException(InvalidStateExMessage(card), nameof(card))
        };
    }
    private static ScheduleInfo ProcessEasy(IScheduleInfoCard card, DeckOptions.SchedulingOpt s)
    {
        return new(
            State: CardState.Review,
            LearningStage: null,

            Interval: (card.State == CardState.New)
                ? TimeSpan.FromDays(s.EasyOnNewDayCount)

            : (card.State == CardState.Learning)
                ? TimeSpan.FromDays(s.GraduateDayCount)
                : card.Interval * s.EasyMultiplier
        );
    }
    private static ScheduleInfo ProcessGood(IScheduleInfoCard card, DeckOptions.SchedulingOpt s)
    {
        return card.State switch
        {
            CardState.New => new(
                LearningStage: s.GoodOnNewStage,
                State: CardState.Learning,
                Interval: s.LearningStages[s.GoodOnNewStage]
            ),

            CardState.Learning => ProcessGoodIfLearningStage(card, s),

            CardState.Review => new(
                Interval: card.Interval * s.GoodMultiplier,
                State: CardState.Review,
                LearningStage: null
            ),
            
            _ => throw new ArgumentException(InvalidStateExMessage(card), nameof(card))
        };
    }
    
    // had to be separate method bc its a whole another world of things to consider, would get too messy.
    private static ScheduleInfo ProcessGoodIfLearningStage(IScheduleInfoCard card, DeckOptions.SchedulingOpt s)
    {
        LearningStage? learningStage = (card.LearningStage is LearningStage.III) // if its already last learning stage:
            ? null                     // set to null
            : card.LearningStage + 1;  // else just add one //TODO: idk if this is gonna work, test it

        CardState state = (learningStage is null)
            ? CardState.Review
            : CardState.Learning;

        TimeSpan interval = (state == CardState.Learning)
            ? s.LearningStages[learningStage]
            : TimeSpan.FromDays(s.GraduateDayCount);


        return new(
            LearningStage: learningStage,
            State: state,
            Interval: interval
        );
    }
    private static ScheduleInfo ProcessAgain(IScheduleInfoCard card, DeckOptions.SchedulingOpt s)
    {
        return card.State switch 
        {
            CardState.Review => new(
                State: CardState.Learning,
                Interval: s.LearningStages[s.AgainOnReviewStage],
                LearningStage: s.AgainOnReviewStage
            ),
        
            CardState.New or CardState.Learning => new(
                State: CardState.Learning,
                Interval: s.LearningStages.I,
                LearningStage: LearningStage.I
            ),

            _ => throw new ArgumentException(InvalidStateExMessage(card), nameof(card))
        };
    }
    #endregion
    private static string InvalidStateExMessage(IScheduleInfoCard si)
        => $"card has invalid state enum value ({si.State})";
}

public record SchedulePermutations(
    ScheduleInfo Easy,
    ScheduleInfo Good, 
    ScheduleInfo Hard, 
    ScheduleInfo Again
);