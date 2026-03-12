using CommunityToolkit.Mvvm.ComponentModel;
using FlashMemo.Model.Domain;

namespace FlashMemo.ViewModel.Wrappers;

public partial class LearningStagesVM: ObservableObject
{
    public LearningStagesVM(LearningStages ls)
    {
        I = ls.I.Minutes;
        II = ls.II.Minutes;
        III = ls.III.Minutes;
    }

    public LearningStagesVM(){} // for json serializer

    [ObservableProperty] public partial int I { get; set; }
    [ObservableProperty] public partial int II { get; set; }
    [ObservableProperty] public partial int III { get; set; }

    public int MinValueOnAnyStage => 1;
    public int MaxValueOnAnyStage => 60;
}