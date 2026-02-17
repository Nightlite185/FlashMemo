using CommunityToolkit.Mvvm.ComponentModel;
using FlashMemo.Model.Domain;
using FlashMemo.Model.Persistence;
using FlashMemo.Services;

namespace FlashMemo.ViewModel.Wrappers;

public partial class CardsCountVM: ObservableObject
{
    public static explicit operator CardsCountVM(CardsCount cc)
    {
        return new()
        {
            Lessons = cc.Lessons,
            Learning = cc.Learning,
            Reviews = cc.Reviews
        };
    }
    public static explicit operator CardsCountVM(CardsByState cbs)
    {
        return new()
        {
            Lessons = cbs.Lessons.Count,
            Learning = cbs.Learning.Count,
            Reviews = cbs.Reviews.Count
        };
    }
    
    [ObservableProperty] public partial int Lessons { get; set; }
    [ObservableProperty] public partial int Learning { get; set; }
    [ObservableProperty] public partial int Reviews { get; set; }

    public void UpdateCount(IEnumerable<CardEntity> cards, int learningCount)
    {
        foreach (var kvp in cards.CountBy(c => c.State))
        {
            switch (kvp.Key)
            {
                case CardState.New:
                    Lessons = kvp.Value;
                    break;

                case CardState.Learning:
                    Learning = kvp.Value;
                    break;

                case CardState.Review:
                    Reviews = kvp.Value;
                    break;
            }
        }

        Learning += learningCount;
    }
}