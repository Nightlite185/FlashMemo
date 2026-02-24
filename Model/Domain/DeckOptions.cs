using System.Collections.Immutable;
using FlashMemo.Model.Persistence;

namespace FlashMemo.Model.Domain;

public class DeckOptions
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

            Decks = [],
            
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

    public sealed class SchedulingOpt
    {
        #region defaults
        public const int LearningStagesCount = 3;

        public const double DefGoodMultiplier = 2; 
        public const double DefEasyMultiplier = 3; 
        public const double DefHardMultiplier = 0; 
        public const int DefGraduateDayCount = 1; 
        public const int DefEasyOnNewDayCount = 2; 
        public const int DefAgainOnReviewStage = 1; 
        public static readonly ImmutableArray<TimeSpan> DefLearningStages = [
            TimeSpan.FromMinutes(4),
            TimeSpan.FromMinutes(8),
            TimeSpan.FromMinutes(15)
        ];
        public const int DefGoodOnNewStage = 2; 
        public const int DefHardOnNewStage = 1; 

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
        public double GoodMultiplier { get; init; } //* a number that your interval is multiplied by when clicking good on a review card.
        public double EasyMultiplier { get; init; } //* a number that your interval is multiplied by when clicking easy on a review card.
        public double HardMultiplier { get; init; } //* a number that your interval is multiplied by when clicking hard on a review card.
        public ImmutableArray<TimeSpan> LearningStages { get; init; } //* all those are in minutes 
        public int GraduateDayCount { get; init; } //* updated interval in days when passing the last stage of learning, into review state (by either answering good on last learning stage or easy or any learning stage).
        public int EasyOnNewDayCount { get; init; } //* updated interval in days after clicking easy on a new card.
        public int GoodOnNewStage { get; init; } //* learning stage you land on after clicking good on a new card.
        public int AgainOnReviewStage { get; init; } //* learning stage you land on, when you click again on a review card.
        public int HardOnNewStage { get; init; } //* learning stage you land on after clicking hard on a new card.
        #endregion

        #region Equals
        // this one is also needed bc ImmutableArray's default Equals implementation is broken.
        public override bool Equals(object? obj)
        {
            if (ReferenceEquals(this, obj))
                return true;

            if (obj is not SchedulingOpt other)
                return false;

            return
                GoodMultiplier == other.GoodMultiplier &&
                EasyMultiplier == other.EasyMultiplier &&
                HardMultiplier == other.HardMultiplier &&
                GraduateDayCount == other.GraduateDayCount &&
                EasyOnNewDayCount == other.EasyOnNewDayCount &&
                GoodOnNewStage == other.GoodOnNewStage &&
                AgainOnReviewStage == other.AgainOnReviewStage &&
                HardOnNewStage == other.HardOnNewStage &&
                LearningStages.SequenceEqual(other.LearningStages);
        }
        public override int GetHashCode()
        {
            var hash = new HashCode();

            hash.Add(GoodMultiplier);
            hash.Add(EasyMultiplier);
            hash.Add(HardMultiplier);

            hash.Add(GraduateDayCount);
            hash.Add(EasyOnNewDayCount);
            hash.Add(GoodOnNewStage);
            hash.Add(AgainOnReviewStage);
            hash.Add(HardOnNewStage);

            // LearningStages
            foreach (var stage in LearningStages)
                hash.Add(stage);

            return hash.ToHashCode();
        }
        #endregion
    }
    public sealed record DailyLimitsOpt
    {
        #region defaults
        public static readonly DailyLimitsOpt Default = new()
        {
            //NewIgnoreReviewLimit = DefNewIgnoreReviewLimit,
            DailyReviewsLimit = DefDailyReviewsLimit,
            DailyLessonsLimit = DefDailyLessonsLimit
        };
        //public const bool DefNewIgnoreReviewLimit = true;
        public const int DefDailyReviewsLimit = 20;
        public const int DefDailyLessonsLimit = 10;
        #endregion

        #region options
        // public bool NewIgnoreReviewLimit { get; init; }
        public int DailyReviewsLimit { get; init; }
        public int DailyLessonsLimit { get; init; }
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

    #region Equals override
    // I need this cuz record auto-equals is gonna diff decks as well 
    // but thats irrelevant to logical comparison.
    public override bool Equals(object? obj)
    {
        if (ReferenceEquals(this, obj))
            return true;

        if (obj is not DeckOptions other)
            return false;

        return
            Equals(DailyLimits, other.DailyLimits) &&
            Equals(Scheduling, other.Scheduling) &&
            Equals(Sorting, other.Sorting) &&
            Id == other.Id &&
            Nullable.Equals(UserId, other.UserId) &&
            string.Equals(Name, other.Name, StringComparison.Ordinal);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(
            Id, Name, UserId, 
            Scheduling, DailyLimits, 
            Sorting);
    }
    #endregion
}