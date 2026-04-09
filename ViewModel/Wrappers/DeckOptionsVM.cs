using System.ComponentModel.DataAnnotations;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FlashMemo.Model.Domain;
using FlashMemo.Model.Persistence;
using FlashMemo.ViewModel.Bases;

namespace FlashMemo.ViewModel.Wrappers;

public partial class DeckOptionsVM: RenameVMBase
{
    public long Id { get; set; }
    public bool CanDelete => Id != DeckOptions.DefaultId;
    
    [ObservableProperty] public partial int DeckCount { get; set; }
    [ObservableProperty] public partial SortingOpt Sorting { get; set; }
    [ObservableProperty] public partial SchedulingOpt Scheduling { get; set; }
    [ObservableProperty] public partial DailyLimitsOpt DailyLimits { get; set; }

    public partial class SortingOpt: ObservableObject, IViewModel
    {
        [ObservableProperty] public partial LessonOrder LessonsOrder { get; set; }
        [ObservableProperty] public partial ReviewOrder ReviewsOrder { get; set; }
        [ObservableProperty] public partial CardStateOrder CardStateOrder { get; set; }

        [ObservableProperty] [NotifyPropertyChangedFor(nameof(ReviewsDirArrow))]
        [NotifyPropertyChangedFor(nameof(ReviewsDirTooltip))]
        public partial SortingDirection ReviewsSortDir { get; set; }
        
        [ObservableProperty] [NotifyPropertyChangedFor(nameof(LessonsDirArrow))]
        [NotifyPropertyChangedFor(nameof(LessonsDirTooltip))]
        public partial SortingDirection LessonsSortDir { get; set; }

        public string LessonsDirTooltip => LessonsSortDir.ToString();
        public string ReviewsDirTooltip => ReviewsSortDir.ToString();
        public string LessonsDirArrow =>
            LessonsSortDir == SortingDirection.Ascending
            ? "︽" : "︾";

        public string ReviewsDirArrow =>
            ReviewsSortDir == SortingDirection.Ascending
            ? "︽" : "︾";


        [RelayCommand]
        private void ToggleLessonsDir()
        {
            LessonsSortDir = (LessonsSortDir == SortingDirection.Ascending)
                ? SortingDirection.Descending
                : SortingDirection.Ascending;
        }

        [RelayCommand]
        private void ToggleReviewsDir()
        {
            ReviewsSortDir = (ReviewsSortDir == SortingDirection.Ascending)
                ? SortingDirection.Descending
                : SortingDirection.Ascending;
        }
    }
    public partial class SchedulingOpt: ObservableValidator, IViewModel
    {
        #region multipliers
        [ObservableProperty] [NotifyDataErrorInfo] [Range(1d, 5d)]
        public partial double GoodMultiplier { get; set; }


        [ObservableProperty] [NotifyDataErrorInfo] [Range(1d, 8d)]
        public partial double EasyMultiplier { get; set; }


        [ObservableProperty] [NotifyDataErrorInfo] [Range(0.1d, 2d)]
        public partial double HardMultiplier { get; set; }
        #endregion
        public LearningStagesVM LearningStages { get; set; } = null!;
        [ObservableProperty] public partial LearningStage AgainOnReviewStage { get; set; }
        [ObservableProperty] public partial LearningStage GoodOnNewStage { get; set; }
        [ObservableProperty] public partial LearningStage HardOnNewStage { get; set; }
        
        [ObservableProperty] [NotifyDataErrorInfo] [Range(1, 30)]
        public partial int GraduateDayCount { get; set; }
        
        [ObservableProperty] [NotifyDataErrorInfo] [Range(1, 30)]
        public partial int EasyOnNewDayCount { get; set; }
    }
    public partial class DailyLimitsOpt: ObservableObject, IViewModel
    {
        [ObservableProperty] public partial int Reviews { get; set; }
        [ObservableProperty] public partial int Lessons { get; set; }
    }
}