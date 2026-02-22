using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using FlashMemo.Model.Domain;
using FlashMemo.Model.Persistence;

namespace FlashMemo.ViewModel.Wrappers;

public partial class DeckOptionsVM: ObservableObject
{
    public long Id { get; set; }
    public bool CanDelete => Id != DeckOptions.DefaultId;
    public List<long> Decks { get; set; } = [];
    public IReadOnlyCollection<long> AssignedDeckIds => [..Decks];
    [ObservableProperty] public partial string Name { get; set; }

    //? consider replacing this with a derived property from deck ids list on the vm.
    [ObservableProperty] public partial int AssignedDecksCount { get; set; }

    [ObservableProperty] public partial SortingOpt Sorting { get; set; }
    [ObservableProperty] public partial SchedulingOpt Scheduling { get; set; }
    [ObservableProperty] public partial DailyLimitsOpt DailyLimits { get; set; }

    public partial class SortingOpt: ObservableObject, IViewModel
    {
        [ObservableProperty] public partial LessonOrder LessonsOrder { get; set; }
        [ObservableProperty] public partial ReviewOrder ReviewsOrder { get; set; }
        [ObservableProperty] public partial SortingDirection ReviewsSortDir { get; set; }
        [ObservableProperty] public partial SortingDirection LessonsSortDir { get; set; }
        [ObservableProperty] public partial CardStateOrder CardStateOrder { get; set; }
    }
    public partial class SchedulingOpt: ObservableObject, IViewModel
    {
        [ObservableProperty] public partial float GoodMultiplier { get; set; }
        [ObservableProperty] public partial float EasyMultiplier { get; set; }
        [ObservableProperty] public partial float HardMultiplier { get; set; }
        [ObservableProperty] public partial ObservableCollection<int> LearningStages { get; set; } = null!; // in minutes
        [ObservableProperty] public partial int GraduateDayCount { get; set; }
        [ObservableProperty] public partial int AgainOnReviewStage { get; set; }
        [ObservableProperty] public partial int GoodOnNewStage { get; set; }
        [ObservableProperty] public partial int EasyOnNewDayCount { get; set; }
        [ObservableProperty] public partial int HardOnNewStage { get; set; }
    }
    public partial class DailyLimitsOpt: ObservableObject, IViewModel
    {
        [ObservableProperty] public partial int DailyReviewsLimit { get; set; }
        [ObservableProperty] public partial int DailyLessonsLimit { get; set; }
    }
}