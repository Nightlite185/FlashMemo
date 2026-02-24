using CommunityToolkit.Mvvm.ComponentModel;

namespace FlashMemo.ViewModel.Wrappers;

public partial class LearningStagesVM: ObservableObject
{
    private static void ThrowIfNot3<T>(IList<T> stages)
    {
        if (stages.Count != 3) throw new ArgumentException(
            "there must be always exactly 3 stages", nameof(stages));
    }
    
    public LearningStagesVM(IList<TimeSpan> stages)
    {
        ThrowIfNot3(stages);

        First = stages[0].Minutes;
        Second = stages[1].Minutes;
        Third = stages[2].Minutes;
    }

    public LearningStagesVM(IList<int> stages)
    {
        ThrowIfNot3(stages);

        First = stages[0];
        Second = stages[1];
        Third = stages[2];
    }

    public LearningStagesVM() // for json serializer
    {

    }

    [ObservableProperty] public partial int First { get; set; }
    [ObservableProperty] public partial int Second { get; set; }
    [ObservableProperty] public partial int Third { get; set; }

    public int MaxValueOnAnyStage { get; } = 60;
    public int MinValueOnAnyStage { get; } = 1;
}