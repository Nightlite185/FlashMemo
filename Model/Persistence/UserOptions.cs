namespace FlashMemo.Model.Persistence;

public record UserOptions
{
    #region defaults
    public const byte DefDayStartTime = 0;
    public const bool DefShowReviewTimer = false;
    public const bool DefTimerStopsOnReveal = true;
    public const bool DefIncludeLessonsInReviewLimit = false;
    public const bool DefIntervalScalingOnOverdueness = true;
    public const bool DefShowHeatmap = true;

    public const byte MaxDayStartOffset = 12;
    #endregion
   
    #region properties
    public byte DayStartOffset { get; init; } //* how many hours past midnight need to pass, for scheduler to consider it the next day.
    public bool ShowReviewTimer { get; init; } //* show timer or not, effective for all decks.
    public bool TimerStopsOnReveal { get; init; } //* whether timer stops on answer being revealed or actually clicking one of the answer buttons.
    public bool IncludeLessonsInReviewLimit { get; init; }
    public bool IntervalScalingOnOverdueness { get; init; } //* whether card's interval will increase scaling off its current overdueness
    public bool ShowHeatmap { get; init; } //* whether to show review heatmap on DecksUC screen
    #endregion

    public static UserOptions CreateDefault()
    {
        return new()
        {
            DayStartOffset = DefDayStartTime,
            ShowReviewTimer = DefShowReviewTimer,
            TimerStopsOnReveal = DefTimerStopsOnReveal,
            IncludeLessonsInReviewLimit = DefIncludeLessonsInReviewLimit,
            IntervalScalingOnOverdueness = DefIntervalScalingOnOverdueness,
            ShowHeatmap = DefShowHeatmap
        };
    }
}