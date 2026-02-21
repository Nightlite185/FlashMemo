using FlashMemo.Model;
using FlashMemo.Model.Domain;

namespace FlashMemo.Services;
   
public static class Scheduler
{
    public static SchedulePermutations GetForecast(ICard card, DeckOptions.SchedulingOpt options)
    {
        return new(
            Easy: ProcessEasy(card, options),
            Good: ProcessGood(card, options),
            Hard: ProcessHard(card, options),
            Again: ProcessAgain(card, options)
        );
    }
    
    #region Private answer handlers
    private static ScheduleInfo ProcessHard(ICard card, DeckOptions.SchedulingOpt s)
    {
        return card.State switch
        {
            CardState.New => new(
                state: CardState.Learning,
                learningStage: s.HardOnNewStage,
                interval: s.LearningStages[s.HardOnNewStage]
            ),
            
            CardState.Learning => new(
                learningStage: card.LearningStage,
                interval: s.LearningStages[(int)card.LearningStage!],
                state: card.State
            ),
            
            CardState.Review => new(
                interval: card.Interval * s.HardMultiplier,
                state: card.State,
                learningStage: null
            ),

            _ => throw new ArgumentException(InvalidStateExMessage(card), nameof(card))
        };
    }
    private static ScheduleInfo ProcessEasy(ICard card, DeckOptions.SchedulingOpt s)
    {
        return new(
            state: CardState.Review,
            learningStage: null,

            interval: (card.State == CardState.New)
                ? TimeSpan.FromDays(s.EasyOnNewDayCount)

            : (card.State == CardState.Learning)
                ? TimeSpan.FromDays(s.GraduateDayCount)
                : card.Interval * s.EasyMultiplier
        );
    }
    private static ScheduleInfo ProcessGood(ICard card, DeckOptions.SchedulingOpt s)
    {
        return card.State switch
        {
            CardState.New => new(
                learningStage: s.GoodOnNewStage,
                state: CardState.Learning,
                interval: s.LearningStages[s.GoodOnNewStage]
            ),

            CardState.Learning => ProcessGoodIfLearningStage(card, s),

            CardState.Review => new(
                interval: card.Interval * s.GoodMultiplier,
                state: CardState.Review,
                learningStage: null
            ),
            
            _ => throw new ArgumentException(InvalidStateExMessage(card), nameof(card))
        };
    }
    
    // had to be separate method bc its a whole another world of things to consider, would get too messy.
    private static ScheduleInfo ProcessGoodIfLearningStage(ICard card, DeckOptions.SchedulingOpt s)
    {
        int? learningStage = (card.LearningStage == s.LearningStages.Length -1) // if its already last learning stage:
            ? null                              // set to null
            : card.LearningStage + 1;           // else just add one 

        CardState state = (learningStage is null)
            ? CardState.Review
            : CardState.Learning;

        TimeSpan interval = (state == CardState.Learning)
            ? s.LearningStages[(int)learningStage!]
            : TimeSpan.FromDays(s.GraduateDayCount);


        return new(
            learningStage: learningStage,
            state: state,
            interval: interval
        );
    }
    private static ScheduleInfo ProcessAgain(ICard card, DeckOptions.SchedulingOpt s)
    {
        return card.State switch 
        {
            CardState.Review => new(
                state: CardState.Learning,
                interval: s.LearningStages[s.AgainOnReviewStage],
                learningStage: s.AgainOnReviewStage
            ),
        
            CardState.New or CardState.Learning => new(
                state: CardState.Learning,
                interval: s.LearningStages[0],
                learningStage: 0
            ),

            _ => throw new ArgumentException(InvalidStateExMessage(card), nameof(card))
        };
    }
    #endregion
    private static string InvalidStateExMessage(ICard c)
        => $"card has invalid state enum value ({c.State}), card's id = {c.Id}";
}

public record SchedulePermutations(
    ScheduleInfo Easy,
    ScheduleInfo Good, 
    ScheduleInfo Hard, 
    ScheduleInfo Again
);