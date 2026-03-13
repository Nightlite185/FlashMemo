using FlashMemo.Helpers;
using FlashMemo.Model.Persistence;

namespace FlashMemo.Model.Domain;

public record DeckOptions
{
    public const sbyte DefaultId = -1;
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
    public static DeckOptions CreateNew(string name, long userId) // factory for new presets, based on default one.
    {
        return Default with
        { 
            Id = IdGetter.Next(), 
            Name = name, 
            UserId = userId
        };
    }

    public long Id { get; init; }
    public string Name { get; init; } = null!;
    public long? UserId { get; init; }
    
    #region sub-options properties
    public SchedulingOpt Scheduling { get; init; } = null!;
    public DailyLimitsOpt DailyLimits { get; init; } = null!;
    public SortingOpt Sorting { get; init; } = null!;
    #endregion

    public sealed record SchedulingOpt
    {
        #region defaults
        public const double DefGoodMultiplier = 1.8;
        public const double DefEasyMultiplier = 2.4;
        public const double DefHardMultiplier = 0.7;
        public const int DefGraduateDayCount = 1; 
        public const int DefEasyOnNewDayCount = 2; 
        public static readonly LearningStages DefLearningStages = new(4, 8, 15);
        public const LearningStage DefAgainOnReviewStage = LearningStage.II;
        public const LearningStage DefGoodOnNewStage = LearningStage.III;
        public const LearningStage DefHardOnNewStage = LearningStage.II;

        public static readonly SchedulingOpt Default = new()
        {
            GoodMultiplier = DefGoodMultiplier,
            EasyMultiplier = DefEasyMultiplier,
            HardMultiplier = DefHardMultiplier,
            GraduateDayCount = DefGraduateDayCount,
            EasyOnNewDayCount = DefEasyOnNewDayCount,
            AgainOnReviewStage = DefAgainOnReviewStage,
            LearningStages = DefLearningStages,
            GoodOnNewStage = DefGoodOnNewStage,
            HardOnNewStage = DefHardOnNewStage
        };
        #endregion
        
        #region options
        public double GoodMultiplier { get; init; } //* a number that a "review" card's interval is multiplied by, after answering "good" on a it.
        public double EasyMultiplier { get; init; } //* a number that a "review" card's interval is multiplied by, after answering "easy" on a it.
        public double HardMultiplier { get; init; } //* a number that a "review" card's interval is multiplied by, after answering "hard" on a it.
        public LearningStages LearningStages { get; init; } = null!; //* there are 3 learning stages that cards go through before they graduate and become a "review" card (all of them are in minutes).
        public int GraduateDayCount { get; init; } //* new interval in days when graduating from learning into "review" state (by either answering "good" on the last learning stage, or "easy" or any learning stage).
        public int EasyOnNewDayCount { get; init; } //* updated interval in days, after answering "easy" on a new card.
        public LearningStage GoodOnNewStage { get; init; } //* learning stage that a "new" card lands on, after answering "good" on it.
        public LearningStage AgainOnReviewStage { get; init; } //* learning stage that a "review" card lands on, after answering "again" on it.
        public LearningStage HardOnNewStage { get; init; } //* learning stage that a "new" card lands on, after answering "hard" on a it.
        #endregion
    }
    public sealed record DailyLimitsOpt
    {
        #region defaults
        public static readonly DailyLimitsOpt Default = new()
        {
            Reviews = DefReviews,
            Lessons = DefLessons
        };
        public const int DefReviews = 20;
        public const int DefLessons = 10;
        #endregion

        #region options
        public int Reviews { get; init; }
        public int Lessons { get; init; }
        #endregion
    }
    public sealed record SortingOpt
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