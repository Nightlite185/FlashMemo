using CommunityToolkit.Mvvm.ComponentModel;
using FlashMemo.Model.Persistence;

namespace FlashMemo.ViewModel.WrapperVMs;

public partial class OrderingOptVM: ObservableObject, IViewModel
{
    [ObservableProperty] public partial LessonOrder LessonsOrder { get; set; }
    [ObservableProperty] public partial ReviewOrder ReviewsOrder { get; set; }
    [ObservableProperty] public partial SortingDirection ReviewsSortDir { get; set; }
    [ObservableProperty] public partial SortingDirection LessonsSortDir { get; set; }
    [ObservableProperty] public partial CardStateOrder CardStateOrder { get; set; }
}

public partial class SchedulingOptVM: ObservableObject, IViewModel
{
    [ObservableProperty] public partial float GoodMultiplier { get; set; }
    [ObservableProperty] public partial float EasyMultiplier { get; set; }
    [ObservableProperty] public partial float HardMultiplier { get; set; }
    [ObservableProperty] public partial int AgainDayCount { get; set; }
    [ObservableProperty] public partial List<TimeSpan> LearningStages { get; set; } = null!; // in minutes
    [ObservableProperty] public partial int AgainStageFallback { get; set; }
    [ObservableProperty] public partial int GoodOnNewStage { get; set; }
    [ObservableProperty] public partial int EasyOnNewDayCount { get; set; }
    [ObservableProperty] public partial int HardOnNewStage { get; set; }
}

public partial class DailyLimitsOptVM: ObservableObject, IViewModel
{
    [ObservableProperty] public partial int DailyReviewsLimit { get; set; }
    [ObservableProperty] public partial int DailyLessonsLimit { get; set; }
}