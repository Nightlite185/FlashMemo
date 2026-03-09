namespace FlashMemo.Model.Persistence;

public class UserOptions
{
    #region defaults
    public static readonly TimeOnly DefDayStartTime = new(0, 0);
    public const bool DefShowReviewTimer = false;
    public const bool DefTimerStopsOnReveal = true;
    public const bool DefIncludeLessonsInReviewLimit = false;
    #endregion
   
    #region properties
    public TimeOnly DayStartTime { get; set; } //* hour between 12am and 12pm after which the SRS system will consider it as the next day.
    public bool ShowReviewTimer { get; set; } //* show timer or not, effective for all decks.
    public bool TimerStopsOnReveal { get; set; } //* whether timer stops on answer being revealed or actually clicking one of the answer buttons.
    public bool IncludeLessonsInReviewLimit { get; set; }
    #endregion

    public static UserOptions CreateDefault()
    {
        return new()
        {
            DayStartTime = DefDayStartTime,
            ShowReviewTimer = DefShowReviewTimer,
            TimerStopsOnReveal = DefTimerStopsOnReveal,
            IncludeLessonsInReviewLimit = DefIncludeLessonsInReviewLimit
        };
    }

    //TODO: replace this with some immutable record for auto equals and hashcode at some point.
    public override bool Equals(object? obj)
    {
        if (obj is not UserOptions valid)
            return false;
        
        return DayStartTime.Equals(valid.DayStartTime)
            && ShowReviewTimer == valid.ShowReviewTimer
            && TimerStopsOnReveal == valid.TimerStopsOnReveal
            && IncludeLessonsInReviewLimit == valid.IncludeLessonsInReviewLimit;
    }
    
    public override int GetHashCode()
    {
        return HashCode.Combine(
            DayStartTime, 
            ShowReviewTimer, 
            TimerStopsOnReveal, 
            IncludeLessonsInReviewLimit);
    }
}