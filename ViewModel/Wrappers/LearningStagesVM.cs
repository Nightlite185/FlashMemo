using System.ComponentModel.DataAnnotations;
using CommunityToolkit.Mvvm.ComponentModel;
using FlashMemo.Model.Domain;

namespace FlashMemo.ViewModel.Wrappers;

public partial class LearningStagesVM: ObservableValidator
{
    public LearningStagesVM(LearningStages ls)
    {
        I = (int)ls.I.TotalMinutes;
        II = (int)ls.II.TotalMinutes;
        III = (int)ls.III.TotalMinutes;
    }

    public LearningStagesVM(){} //* for json serializer

    public bool Validate()
    {
        ValidateAllProperties();
        return !HasErrors;
    }

    [ObservableProperty] [NotifyDataErrorInfo] [Range(1, 60)]
    public partial int I { get; set; }

    [ObservableProperty] [NotifyDataErrorInfo] [Range(1, 60)]
    public partial int II { get; set; }

    [ObservableProperty] [NotifyDataErrorInfo] [Range(1, 60)]
    public partial int III { get; set; }
}