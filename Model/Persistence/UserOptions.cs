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
}