using System.Collections.Immutable;

namespace FlashMemo.Model
{
    public class Scheduler: IDefaultable
    {
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
        public float GoodMultiplier { get; set; } = DefGoodMultiplier;
        public float EasyMultiplier { get; set; } = DefEasyMultiplier;
        public float HardMultiplier { get; set; } = DefHardMultiplier;
        public int AgainDayCount { get; set; } = DefAgainDayCount;
        public List<TimeSpan> LearningStages { get; set; } = [..DefLearningStages];
        public int AgainStageFallback { get; set; } = DefAgainOnReviewStage;
        public int GoodOnNewStage { get; set; } = DefGoodOnNewStage;
        public int EasyOnNewDayCount { get; set; } = DefEasyOnNewDayCount;
        public int HardOnNewStage { get; set; } = DefHardOnNewStage;
        #endregion
        public void ScheduleCard(Card card, Answers answer) // this should call priv methods that separately handle each answer (depending on it) - to encapsulate and clean the logic a bit.
        {
            TimeSpan interval;
            CardState state = card.State;
            int? learningStage = null;

            if (answer == Answers.Easy)
            {
                state = CardState.Review;

                interval = (card.State == CardState.New)
                    ? new TimeSpan(days: EasyOnNewDayCount, 0, 0, 0)
                : (card.State == CardState.Learning)
                    ? new TimeSpan(days: (int)Math.Round(EasyMultiplier, MidpointRounding.AwayFromZero), 0, 0, 0)
                    : card.Interval * EasyMultiplier;

                learningStage = null;
            }

            else if (answer == Answers.Again)
            {
                if (card.State == CardState.Review)
                {
                    state = CardState.Learning;
                    interval = LearningStages[AgainStageFallback];
                    learningStage = AgainStageFallback;
                }

                else if (card.State is CardState.New or CardState.Learning)
                {
                    state = CardState.Learning;
                    interval = LearningStages[0];
                    learningStage = 0;
                }

                else throw new Exception($"card has invalid state - {card.State}");
            }

            else if (answer == Answers.Good)
            {
                if (card.State == CardState.New)
                {
                    learningStage = GoodOnNewStage;
                    state = CardState.Learning;
                    interval = LearningStages[GoodOnNewStage];
                }
                
                else if (card.State == CardState.Learning)
                {
                    learningStage = (card.LearningStage == 2) // if its already last learning stage:
                        ? null                              // set to null
                        : card.LearningStage + 1;           // else just add one 

                    state = (card.LearningStage == null)
                        ? CardState.Review
                        : CardState.Learning;

                    interval = (state == CardState.Learning)
                        ? LearningStages[(int)learningStage!]
                        : new TimeSpan(days: (int)Math.Round(GoodMultiplier, MidpointRounding.AwayFromZero), 0, 0, 0);
                }

                else // state = review
                    interval = card.Interval * GoodMultiplier;
            }

            else // answer = hard
            {
                if (card.State == CardState.New)
                {
                    state = CardState.Learning;
                    learningStage = HardOnNewStage;
                    interval = LearningStages[HardOnNewStage];
                }
            
                else if (card.State == CardState.Learning)
                {
                    learningStage = card.LearningStage;
                    interval = LearningStages[(int)learningStage!];
                }
            
                else // state = review
                {
                    interval = card.Interval * HardMultiplier;
                }
            }

            card.Review(interval, state, learningStage);
        }
    }
}