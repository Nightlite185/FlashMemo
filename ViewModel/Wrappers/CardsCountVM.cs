using CommunityToolkit.Mvvm.ComponentModel;
using FlashMemo.Model.Domain;
using FlashMemo.Services;

namespace FlashMemo.ViewModel.Wrappers;

public partial class CardsCountVM: ObservableObject
{
    private ICardsSource<CardVM> source;

    public CardsCountVM(CardsCount cc, ICardsSource<CardVM> source)
    {
        this.source = source;

        Lessons = cc.Lessons;
        Learning = cc.Learning;
        Reviews = cc.Reviews;
    }
    public CardsCountVM(CardsByState cbs, ICardsSource<CardVM> source)
    {
        this.source = source;

        Lessons = cbs.Lessons.Count;
        Learning = cbs.Learning.Count;
        Reviews = cbs.Reviews.Count;
    }

    public CardsCountVM(ICardsSource<CardVM> source)
        => this.source = source;
    
    [ObservableProperty] public partial int Lessons { get; set; }
    [ObservableProperty] public partial int Learning { get; set; }
    [ObservableProperty] public partial int Reviews { get; set; }

    public void UpdateCount()
    {
        Clear();

        var today = DateTime.Today;

        var inPlay = source.Cards
            .Where(c => !c.IsInvalid)
            .CountBy(c => c.State);

        foreach (var kvp in inPlay)
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
    }

    private void Clear()
    {
        Lessons = 0;
        Reviews = 0;
        Learning = 0;
    }
}