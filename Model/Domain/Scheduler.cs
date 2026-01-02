using System.Collections.Immutable;
using Force.DeepCloner;

namespace FlashMemo.Model.Domain
{
    public struct ScheduleInfo(TimeSpan interval, CardState state, int? learningStage)
    {
        public TimeSpan Interval { get; set; } = interval;
        public CardState State { get; set; } = state;
        public int? LearningStage { get; set; } = learningStage;
    }
    public class Scheduler: IDefaultable
    {
        public Scheduler(long id) => Id = id; // ctor for mapper only
        public Scheduler(string name = "New preset") // default ctor for genuine creation
        {
            Id = IdGetter.Next();
            Name = name;

            GoodMultiplier = DefGoodMultiplier;
            EasyMultiplier = DefEasyMultiplier;
            HardMultiplier = DefHardMultiplier;
            LearningStages = [..DefLearningStages];
            AgainDayCount = DefAgainDayCount;
            AgainStageFallback = DefAgainOnReviewStage;
            GoodOnNewStage = DefGoodOnNewStage;
            EasyOnNewDayCount = DefEasyOnNewDayCount;
            HardOnNewStage = DefHardOnNewStage;
        }
        public string Name { get; private set; } = null!;
        public long Id { get; private set; }
        
        #region defaults
        public const float DefGoodMultiplier = 2.0f;
        public const float DefEasyMultiplier = 3.0f;
        public const float DefHardMultiplier = 0.8f;
        public const int DefAgainDayCount = 1;
        public const int DefEasyOnNewDayCount = 3;
        public static int DefAgainOnReviewStage => 1; // mid stage by default
        public static readonly ImmutableArray<TimeSpan> DefLearningStages = [ // all those are in minutes for now
            new TimeSpan(0,4,0),
            new TimeSpan(0,8,0),
            new TimeSpan(0,15,0)
        ];
        public const int DefGoodOnNewStage = 2; // last one, bc there should be always 3 elements of the learning stages
        public const int DefHardOnNewStage = 1; // clicking hard on new card lands in the middle of learning stages.
        public void ToDefault()
        {
            EasyMultiplier = DefEasyMultiplier;
            GoodMultiplier = DefGoodMultiplier;
            HardMultiplier = DefHardMultiplier;

            LearningStages = [..DefLearningStages];

            AgainDayCount = DefAgainDayCount;
            AgainStageFallback = DefAgainOnReviewStage;
            GoodOnNewStage = DefGoodOnNewStage;
            EasyOnNewDayCount = DefEasyOnNewDayCount;
            HardOnNewStage = DefHardOnNewStage;
        }
        #endregion
        
        #region scheduling parameters
        public float GoodMultiplier { get; private set; }
        public float EasyMultiplier { get; private set; }
        public float HardMultiplier { get; private set; }
        public int AgainDayCount { get; private set; }
        public List<TimeSpan> LearningStages { get; private set; } = null!; // in minutes
        public int AgainStageFallback { get; private set; }
        public int GoodOnNewStage { get; private set; }
        public int EasyOnNewDayCount { get; private set; }
        public int HardOnNewStage { get; private set; }
        #endregion
        public void ScheduleCard(Card card, Answers answer)
        {
            ScheduleInfo info = answer switch
            {
                Answers.Easy => ProcessEasy(card),
                Answers.Good => ProcessGood(card),
                Answers.Hard => ProcessHard(card),
                Answers.Again => ProcessAgain(card),

                _ => throw new ArgumentException($"Invalid value of Answer enum - {answer}", nameof(answer))
            };

            card.Review(info);
        }
        public Scheduler Rehydrate(string name, float goodMultiplier, float easyMultiplier, float hardMultiplier,
                                   IEnumerable<TimeSpan> learningStages, int againDayCount, int againStageFallback, int goodOnNewStage,
                                   int easyOnNewDayCount, int hardOnNewStage)
        {
            Name = name;
            GoodMultiplier = goodMultiplier;
            EasyMultiplier = easyMultiplier;
            HardMultiplier = hardMultiplier;
            LearningStages = [..learningStages];
            AgainDayCount = againDayCount;
            AgainStageFallback = againStageFallback;
            GoodOnNewStage = goodOnNewStage;
            EasyOnNewDayCount = easyOnNewDayCount;
            HardOnNewStage = hardOnNewStage;

            return this;
        }
        public Scheduler Clone(int? HighestCopyNum = null)
        {
            if (HighestCopyNum <= 0) throw new ArgumentOutOfRangeException(nameof(HighestCopyNum));

            var copy = this.DeepClone();

            copy.Name = $"{this.Name} - copy" + (
                HighestCopyNum != null
                    ? $"({HighestCopyNum+1})" 
                    : ""
            );

            return copy;
        }
        
        #region Private answer handlers
        private ScheduleInfo ProcessHard(Card card)
        {
            return card.State switch
            {
                CardState.New => new(
                    state: CardState.Learning,
                    learningStage: HardOnNewStage,
                    interval: LearningStages[HardOnNewStage]
                ),
                
                CardState.Learning => new(
                    learningStage: card.LearningStage,
                    interval: LearningStages[(int)card.LearningStage!],
                    state: card.State
                ),
                
                CardState.Review => new(
                    interval: card.Interval * HardMultiplier,
                    state: card.State,
                    learningStage: null
                ),

                _ => throw new ArgumentException(InvalidStateExMessage(card), nameof(card))
            };
        }
        private ScheduleInfo ProcessEasy(Card card)
        {
            return new(
                state: CardState.Review,
                learningStage: null,

                interval: (card.State == CardState.New)
                    ? new TimeSpan(days: EasyOnNewDayCount, 0, 0, 0)
                : (card.State == CardState.Learning)
                    ? new TimeSpan(days: (int)Math.Round(EasyMultiplier, MidpointRounding.AwayFromZero), 0, 0, 0)
                    : card.Interval * EasyMultiplier
            );
        }
        private ScheduleInfo ProcessGood(Card card)
        {
            return card.State switch
            {
                CardState.New => new(
                    learningStage: GoodOnNewStage,
                    state: CardState.Learning,
                    interval: LearningStages[GoodOnNewStage]
                ),

                CardState.Learning => ProcessGoodIfLearningStage(card),

                CardState.Review => new(
                    interval: card.Interval * GoodMultiplier,
                    state: card.State,
                    learningStage: null
                ),
                
                _ => throw new ArgumentException(InvalidStateExMessage(card), nameof(card))
            };
        }
        private ScheduleInfo ProcessGoodIfLearningStage(Card card)
        {
            /* this case has to be separate cuz next params depend on the previous variables set -> not to repeat the same ifs.
            calling a ctor in switch case can't dynamically depend on previous args since the struct doesnt even exist yet. */

            int? learningStage = (card.LearningStage == 2) // if its already last learning stage:
                ? null                              // set to null
                : card.LearningStage + 1;           // else just add one 

            CardState state = (learningStage == null)
                ? CardState.Review
                : CardState.Learning;

            TimeSpan interval = (state == CardState.Learning)
                ? LearningStages[(int)learningStage!]
                : new TimeSpan(days: (int)Math.Round(GoodMultiplier, MidpointRounding.AwayFromZero), 0, 0, 0);


            return new(
                learningStage: learningStage,
                state: state,
                interval: interval
            );
        }
        private ScheduleInfo ProcessAgain(Card card)
        {
            return card.State switch 
            {
                CardState.Review => new(
                    state: CardState.Learning,
                    interval: LearningStages[AgainStageFallback],
                    learningStage: AgainStageFallback
                ),
            
                CardState.New or CardState.Learning => new(
                    state: CardState.Learning,
                    interval: LearningStages[0],
                    learningStage: 0
                ),

                _ => throw new ArgumentException(InvalidStateExMessage(card), nameof(card))
            };
        }
        #endregion
        private static string InvalidStateExMessage(Card c)
            => $"card has invalid state enum value ({c.State}), card's id = {c.Id}";
    }
}