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
    public long? UserId { get; set; }
    public bool CanDelete => Id != DeckOptions.DefaultId;

    public bool Validate()
        => Sorting.Validate()
        && Scheduling.Validate()
        && DailyLimits.Validate();
    
    [ObservableProperty] public partial int DeckCount { get; set; }
    [ObservableProperty] public partial SortingOpt Sorting { get; set; }
    [ObservableProperty] public partial SchedulingOpt Scheduling { get; set; }
    [ObservableProperty] public partial DailyLimitsOpt DailyLimits { get; set; }

    public partial class SortingOpt: ObservableValidator, IViewModel
    {
        public bool Validate()
        {
            ValidateAllProperties();
            return !HasErrors;
        }

        [ObservableProperty] [NotifyDataErrorInfo] [EnumDataType(typeof(LessonOrder))]
        public partial LessonOrder LessonsOrder { get; set; }

        [ObservableProperty] [NotifyDataErrorInfo] [EnumDataType(typeof(ReviewOrder))]
        public partial ReviewOrder ReviewsOrder { get; set; }
        
        [ObservableProperty] [NotifyDataErrorInfo] [EnumDataType(typeof(CardStateOrder))]
        public partial CardStateOrder CardStateOrder { get; set; }

        [ObservableProperty] [NotifyDataErrorInfo] [EnumDataType(typeof(SortingDirection))]
        public partial SortingDirection ReviewsSortDir { get; set; }
        
        [ObservableProperty] [NotifyDataErrorInfo] [EnumDataType(typeof(SortingDirection))]
        public partial SortingDirection LessonsSortDir { get; set; }

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
        public bool Validate()
        {
            ValidateAllProperties();
            return !HasErrors && LearningStages.Validate();
        }

        #region multipliers
        [ObservableProperty] [NotifyDataErrorInfo] [Range(0.1d, 8d)]
        public partial double GoodMultiplier { get; set; }


        [ObservableProperty] [NotifyDataErrorInfo] [Range(0.1d, 8d)]
        public partial double EasyMultiplier { get; set; }


        [ObservableProperty] [NotifyDataErrorInfo] [Range(0.1d, 8d)]
        public partial double HardMultiplier { get; set; }
        #endregion
        
        #region learning stages
        public LearningStagesVM LearningStages { get; set; } = null!;

        [ObservableProperty] [NotifyDataErrorInfo] [EnumDataType(typeof(LearningStage))]
        public partial LearningStage AgainOnReviewStage { get; set; }

        [ObservableProperty] [NotifyDataErrorInfo] [EnumDataType(typeof(LearningStage))]
        public partial LearningStage GoodOnNewStage { get; set; }

        [ObservableProperty] [NotifyDataErrorInfo] [EnumDataType(typeof(LearningStage))]
        public partial LearningStage HardOnNewStage { get; set; }
        #endregion

        #region day counts
        [ObservableProperty] [NotifyDataErrorInfo] [Range(1, 100)]
        public partial int GraduateDayCount { get; set; }
        
        [ObservableProperty] [NotifyDataErrorInfo] [Range(1, 100)]
        public partial int EasyOnNewDayCount { get; set; }
        #endregion
    }
    public partial class DailyLimitsOpt: ObservableValidator, IViewModel
    {
        public bool Validate()
        {
            ValidateAllProperties();
            return !HasErrors;
        }

        [ObservableProperty] [NotifyDataErrorInfo] [Range(minimum: 0, maximum: 1_000_000)]
        public partial int Reviews { get; set; }

        [ObservableProperty] [NotifyDataErrorInfo] [Range(minimum: 0, maximum: 1_000_000)]
        public partial int Lessons { get; set; }
    }
}