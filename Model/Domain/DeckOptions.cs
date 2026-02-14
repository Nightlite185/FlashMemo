using System.Collections.Immutable;
using FlashMemo.Model.Persistence;

namespace FlashMemo.Model.Domain;

public record class DeckOptions
{
    public static DeckOptions Default { get; } = CreateDefault();
    private static DeckOptions CreateDefault() // Factory for default preset
    {
        return new()
        {
            Name = "Default",
            Id = -1,
            UserId = null,
            
            Scheduling = SchedulingOpt.Default,
            DailyLimits = DailyLimitsOpt.Default,
            Sorting = SortingOpt.Default
        };
    }
    
    public long Id { get; init; }
    public string Name { get; init; } = null!;
    public long? UserId { get; init; }
    public ImmutableArray<long> Decks { get; init; }
    
    #region sub-options properties
    public SchedulingOpt Scheduling { get; init; } = null!;
    public DailyLimitsOpt DailyLimits { get; init; } = null!;
    public SortingOpt Sorting { get; init; } = null!;
    #endregion

    public sealed record class SchedulingOpt
    {
        #region defaults
        public const float DefGoodMultiplier = 2.0f;
        public const float DefEasyMultiplier = 3.0f;
        public const float DefHardMultiplier = 0.8f;
        public const int DefAgainDayCount = 1;
        public const int DefEasyOnNewDayCount = 3;
        public const int DefAgainOnReviewStage = 1; // mid stage by default
        public static readonly ImmutableArray<TimeSpan> DefLearningStages = [ // all those are in minutes for now
            TimeSpan.FromMinutes(4),
            TimeSpan.FromMinutes(8),
            TimeSpan.FromMinutes(15)
        ];
        public const int DefGoodOnNewStage = 2; // last one, bc there should be always 3 elements of the learning stages
        public const int DefHardOnNewStage = 1; // clicking hard on new card lands in the middle of learning stages.

        public static readonly SchedulingOpt Default = new()
        {
            GoodMultiplier = DefGoodMultiplier,
            EasyMultiplier = DefEasyMultiplier,
            HardMultiplier = DefHardMultiplier,
            AgainDayCount = DefAgainDayCount,
            EasyOnNewDayCount = DefEasyOnNewDayCount,
            AgainOnReviewStage = DefAgainOnReviewStage,
            LearningStages = DefLearningStages,
            GoodOnNewStage = DefGoodOnNewStage,
            HardOnNewStage = DefHardOnNewStage
        };
        #endregion
        
        #region options
        public float GoodMultiplier { get; init; }
        public float EasyMultiplier { get; init; }
        public float HardMultiplier { get; init; }
        public int AgainDayCount { get; init; }
        public required ImmutableArray<TimeSpan> LearningStages { get; init; }
        public int EasyOnNewDayCount { get; init; }
        public int GoodOnNewStage { get; init; }
        public int AgainOnReviewStage { get; init; }
        public int HardOnNewStage { get; init; }
        #endregion
    }
    public sealed record class DailyLimitsOpt
    {
        #region defaults
        public static readonly DailyLimitsOpt Default = new()
        {
            NewIgnoreReviewLimit = DefNewIgnoreReviewLimit,
            DailyReviewsLimit = DefDailyReviewsLimit,
            DailyLessonsLimit = DefDailyLessonsLimit
        };
        public const bool DefNewIgnoreReviewLimit = true;
        public const int DefDailyReviewsLimit = 20;
        public const int DefDailyLessonsLimit = 10;
        #endregion

        #region options
        public bool NewIgnoreReviewLimit { get; init; }
        public int DailyReviewsLimit { get; init; }
        public int DailyLessonsLimit { get; init; }
        #endregion
    }
    public record class SortingOpt
    {
        #region defaults
        public const LessonOrder DefLessonSortOrder = LessonOrder.Created;
        public const ReviewOrder DefReviewSortOrder = ReviewOrder.Due;
        public const SortingDirection DefLessonSortDir = SortingDirection.Descending;
        public const SortingDirection DefReviewSortDir = SortingDirection.Descending;
        public const CardStateOrder DefCardTypeOrder = CardStateOrder.ReviewsThenNew;

        public static readonly SortingOpt Default = new()
        {
            LessonSortOrder = DefLessonSortOrder,
            ReviewSortOrder = DefReviewSortOrder,
            LessonSortDir = DefLessonSortDir,
            ReviewSortDir = DefReviewSortDir,
            CardTypeOrder = DefCardTypeOrder
        };
        #endregion

        #region options
        public LessonOrder LessonSortOrder { get; init; }
        public ReviewOrder ReviewSortOrder { get; init; }
        public SortingDirection LessonSortDir { get; init; }
        public SortingDirection ReviewSortDir { get; init; }
        public CardStateOrder CardTypeOrder { get; init; }
        #endregion
    }
}