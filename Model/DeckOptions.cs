using System.Collections.Immutable;
using FlashMemo.Model.Persistence;

namespace FlashMemo.Model
{
    public enum CardTypeOrder { NewThenReviews, ReviewsThenNew, Mix }
    public sealed class DeckOptions: IDefaultable
    {
        public DeckOptions() // ctor for default preset
        {
            Name = "Default";

            Scheduling = new();
            DailyLimits = new();
            Sorting = new();

            ToDefault();
        }
        public string Name { get; set; }
        public long Id { get; set; }
        public long UserId { get; set; }
        public ICollection<DeckEntity> DecksUsingThis { get; set; } = [];
        public void ToDefault()
        {
            Scheduling.ToDefault();
            DailyLimits.ToDefault();
            Sorting.ToDefault();   
        }
        
        #region sub-options properties
        public SchedulingOpt Scheduling { get; set; }
        public DailyLimitsOpt DailyLimits { get; set; }
        public OrderingOpt Sorting { get; set; }
        #endregion

        #region options sub-classes
        public sealed class SchedulingOpt: IDefaultable
        {
            #region defaults
            public const float DefGoodMultiplier = 2.0f;
            public const float DefEasyMultiplier = 3.0f;
            public const float DefHardMultiplier = 0.8f;
            public const int DefAgainDayCount = 1;
            public const int DefEasyOnNewDayCount = 3;
            public static int DefAgainOnReviewStage => 1; // mid stage by default
            public static readonly ImmutableArray<TimeSpan> DefLearningStages = [ // all those are in minutes for now
                TimeSpan.FromMinutes(4),
                TimeSpan.FromMinutes(8),
                TimeSpan.FromMinutes(15)
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
            
            #region options
            public float GoodMultiplier { get; set; }
            public float EasyMultiplier { get; set; }
            public float HardMultiplier { get; set; }
            public int AgainDayCount { get; set; }
            public List<TimeSpan> LearningStages { get; set; } = null!; // in minutes
            public int AgainStageFallback { get; set; }
            public int GoodOnNewStage { get; set; }
            public int EasyOnNewDayCount { get; set; }
            public int HardOnNewStage { get; set; }
            #endregion
        }
        public sealed class DailyLimitsOpt: IDefaultable
        {
            #region defaults
            public const bool DefNewIgnoreReviewLimit = true;
            public const int DefDailyReviewsLimit = 20;
            public const int DefDailyLessonsLimit = 10;
            public void ToDefault()
            {
                NewIgnoreReviewLimit = DefNewIgnoreReviewLimit;
                DailyReviewsLimit = DefDailyReviewsLimit;
                DailyLessonsLimit = DefDailyLessonsLimit;
            }
            #endregion

            #region options
            public static bool NewIgnoreReviewLimit { get; set; } // global option (not per preset)
            public int DailyReviewsLimit { get; set; }
            public int DailyLessonsLimit { get; set; }
            #endregion
        }
        public sealed class OrderingOpt: IDefaultable
        {
            #region defaults
            public const SortingOptions DefNewCardSortOrder = SortingOptions.Created; // TO DO: maybe also split sorting enum into review and lessons sorting.
            public const SortingOptions DefReviewSortOrder = SortingOptions.Random; // TO DO: add sorting option 'overdueness'
            public const CardTypeOrder DefCardTypeOrder = CardTypeOrder.ReviewsThenNew;
            public void ToDefault()
            {
                NewCardSortOrder = DefNewCardSortOrder;
                ReviewSortOrder = DefReviewSortOrder;
                CardTypeOrder = DefCardTypeOrder;
            }
            #endregion

            #region options
            public SortingOptions NewCardSortOrder { get; set; }
            public SortingOptions ReviewSortOrder { get; set; }
            public CardTypeOrder CardTypeOrder { get; set; }
            #endregion
        }
        #endregion
    }
}