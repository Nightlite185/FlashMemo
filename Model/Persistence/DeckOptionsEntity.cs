using System.ComponentModel.DataAnnotations.Schema;
using FlashMemo.Helpers;

namespace FlashMemo.Model.Persistence;   
    
#region enums
public enum CardStateOrder { NewThenReviews, ReviewsThenNew, Mix }

public enum CardsOrder { Created, Id, LastModified, Due, LastReviewed, Interval, State, Random }

public enum LessonOrder { Created, LastModified, Random }

public enum ReviewOrder { Created, Due, Interval, LastModified, LastReviewed, Random }

public enum SortingDirection { Ascending, Descending }
#endregion
public class DeckOptionsEntity
{
    public void NewOwnedTypes()
    {
        Scheduling_ ??= new();
        DailyLimits_ ??= new();
        Ordering_ ??= new();
    }

    [ForeignKey(nameof(UserId))]
    public long Id { get; set; }
    public long UserId { get; set; }
    public UserEntity User { get; set; } = null!;
    public List<Deck> Decks { get; set; } = [];
    public string Name { get; set; } = null!;

    #region sub-options properties
    public Scheduling Scheduling_ { get; set; } = null!;
    public DailyLimits DailyLimits_ { get; set; } = null!;
    public Ordering Ordering_ { get; set; } = null!;
    #endregion

    #region options sub-classes
    public sealed class Scheduling
    {
        public float GoodMultiplier { get; set; }
        public float EasyMultiplier { get; set; }
        public float HardMultiplier { get; set; }
        public int AgainDayCount { get; set; }
        public List<TimeSpan> LearningStages { get; set; } = null!; // in minutes
        public int AgainStageFallback { get; set; }
        public int GoodOnNewStage { get; set; }
        public int EasyOnNewDayCount { get; set; }
        public int HardOnNewStage { get; set; }
    }
    public sealed class DailyLimits
    {
        /* global option (not per preset). // TODO: Later figure out how to persist this bc EF ignores static 
        and I dont wanna create a separate table just for this. Maybe belongs in UserOptions?? */
        // public static bool NewIgnoreReviewLimit { get; set; }
        public int DailyReviewsLimit { get; set; }
        public int DailyLessonsLimit { get; set; }
    }
    public sealed class Ordering
    {
        public LessonOrder LessonsOrder { get; set; }
        public ReviewOrder ReviewsOrder { get; set; }
        public SortingDirection ReviewsSortDir { get; set; }
        public SortingDirection LessonsSortDir { get; set; }
        public CardStateOrder CardStateOrder { get; set; }
    }
    #endregion
}