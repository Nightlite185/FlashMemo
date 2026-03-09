using CommunityToolkit.Mvvm.ComponentModel;

namespace FlashMemo.ViewModel.Wrappers;

public partial class UserOptionsVM: ObservableObject
{
    [ObservableProperty] public partial uint DayStartTime { get; set; }
    [ObservableProperty] public partial bool ShowReviewTimer { get; set; } 
    [ObservableProperty] public partial bool TimerStopsOnReveal { get; set; }
    [ObservableProperty] public partial bool IncludeLessonsInReviewLimit { get; set; }
}