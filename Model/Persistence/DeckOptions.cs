using System.Collections.Immutable;
using System.ComponentModel.DataAnnotations.Schema;

namespace FlashMemo.Model.Persistence
{   
    #region enums
    public enum CardStateOrder { NewThenReviews, ReviewsThenNew, Mix }

    public enum CardsOrder { Created, Id, LastModified, Due, LastReviewed, Interval, State, Random }

    public enum LessonOrder { Created, LastModified, Random }

    public enum ReviewOrder { Created, Due, Interval, LastModified, LastReviewed, Random }

    public enum SortingDirection { Ascending, Descending }
    #endregion
    public class DeckOptions: IDefaultable
    {
        public DeckOptions(){} // ctor for EF
        public static DeckOptions CreateDefault(string name) // Factory for default preset
        {
            DeckOptions o = new()
            {
                Name = name,

                Scheduling = new(),
                DailyLimits = new(),
                Sorting = new()
            };

            o.ToDefault();

            return o;
        }
        
        [ForeignKey(nameof(UserId))]
        public long Id { get; set; }
        public long UserId { get; set; }
        public UserEntity User { get; set; } = null!;
        public ICollection<Deck> DecksUsingThis { get; set; } = [];
        public string Name { get; set; } = null!;
        public void ToDefault()
        {
            Scheduling.ToDefault();
            DailyLimits.ToDefault();
            Sorting.ToDefault();   
        }
        
        #region sub-options properties
        public SchedulingOpt Scheduling { get; set; } = null!;
        public DailyLimitsOpt DailyLimits { get; set; } = null!;
        public OrderingOpt Sorting { get; set; } = null!;
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
            /* global option (not per preset). TO DO: Later figure out how to persist this bc EF ignores static 
            and I dont wanna create a separate table just for this. Maybe belongs in UserOptions?? */
            public static bool NewIgnoreReviewLimit { get; set; } 
            public int DailyReviewsLimit { get; set; }
            public int DailyLessonsLimit { get; set; }
            #endregion
        }
        public sealed class OrderingOpt: IDefaultable
        {
            #region defaults
            public const LessonOrder DefLessonSortOrder = LessonOrder.Created; // TO DO: maybe also split sorting enum into review and lessons sorting.
            public const ReviewOrder DefReviewSortOrder = ReviewOrder.Due;
            public const SortingDirection DefLessonSortDir = SortingDirection.Descending;
            public const SortingDirection DefReviewSortDir = SortingDirection.Descending;
            public const CardStateOrder DefCardTypeOrder = CardStateOrder.ReviewsThenNew;
            public void ToDefault()
            {
                LessonsOrder = DefLessonSortOrder;
                ReviewsOrder = DefReviewSortOrder;

                ReviewsSortDir = DefReviewSortDir;
                LessonsSortDir = DefLessonSortDir;
                
                CardStateOrder = DefCardTypeOrder;
            }
            #endregion

            #region options
            public LessonOrder LessonsOrder { get; set; }
            public ReviewOrder ReviewsOrder { get; set; }
            public SortingDirection ReviewsSortDir { get; set; }
            public SortingDirection LessonsSortDir { get; set; }
            public CardStateOrder CardStateOrder { get; set; }
            #endregion
        }
        #endregion
    }
}