namespace FlashMemo.Model.Persistence;

public class UserOptions
{
    #region defaults
    public const byte DefDayStartTime = 0;
    public const bool DefShowReviewTimer = false;
    public const bool DefTimerStopsOnReveal = true;
    public const bool DefIncludeLessonsInReviewLimit = false;
    public const bool DefIntervalScalingOnOverdueness = true;

    public const byte MaxDayStartOffset = 12;
    #endregion
   
    #region properties
    public byte DayStartOffset { get; set; } //* how many hours past midnight need to pass, for scheduler to consider it the next day.
    public bool ShowReviewTimer { get; set; } //* show timer or not, effective for all decks.
    public bool TimerStopsOnReveal { get; set; } //* whether timer stops on answer being revealed or actually clicking one of the answer buttons.
    public bool IncludeLessonsInReviewLimit { get; set; }
    public bool IntervalScalingOnOverdueness { get; set; } //* whether card's interval will increase scaling off its current overdueness
    #endregion

    public static UserOptions CreateDefault()
    {
        return new()
        {
            DayStartOffset = DefDayStartTime,
            ShowReviewTimer = DefShowReviewTimer,
            TimerStopsOnReveal = DefTimerStopsOnReveal,
            IncludeLessonsInReviewLimit = DefIncludeLessonsInReviewLimit,
            IntervalScalingOnOverdueness = DefIntervalScalingOnOverdueness
        };
    }

    //TODO: replace this with some immutable record for auto equals and hashcode at some point.
    public override bool Equals(object? obj)
    {
        if (obj is not UserOptions valid)
            return false;
        
        return DayStartOffset.Equals(valid.DayStartOffset)
            && ShowReviewTimer == valid.ShowReviewTimer
            && TimerStopsOnReveal == valid.TimerStopsOnReveal
            && IncludeLessonsInReviewLimit == valid.IncludeLessonsInReviewLimit
            && IntervalScalingOnOverdueness == valid.IntervalScalingOnOverdueness;
    }
    
    public override int GetHashCode()
    {
        return HashCode.Combine(
            DayStartOffset, 
            ShowReviewTimer, 
            TimerStopsOnReveal, 
            IncludeLessonsInReviewLimit,
            IntervalScalingOnOverdueness);
    }
}