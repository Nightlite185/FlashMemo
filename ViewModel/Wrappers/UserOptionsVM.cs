using System.ComponentModel.DataAnnotations;
using CommunityToolkit.Mvvm.ComponentModel;

namespace FlashMemo.ViewModel.Wrappers;

public partial class UserOptionsVM: ObservableValidator
{
    [ObservableProperty]
    [NotifyDataErrorInfo]
    [Range(0, 12)]
    public partial uint DayStartTime { get; set; }

    [ObservableProperty] public partial bool ShowReviewTimer { get; set; }
    [ObservableProperty] public partial bool TimerStopsOnReveal { get; set; }
    [ObservableProperty] public partial bool IncludeLessonsInReviewLimit { get; set; }
    [ObservableProperty] public partial bool IntervalScalingOnOverdueness { get; set; }
}