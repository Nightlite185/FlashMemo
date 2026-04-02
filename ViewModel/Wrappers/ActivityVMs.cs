using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;

namespace FlashMemo.ViewModel.Wrappers;

public partial class ActivityCellVM: ObservableObject
{
    [ObservableProperty] [NotifyPropertyChangedFor(nameof(Intensity))]
    public required partial int ReviewCount { get; set; }
    public required DateOnly Date { get; init; }
    public int Intensity => ReviewCount switch
    {
            0 => 0,
        <= 10 => 1,
        <= 20 => 2,
        <= 30 => 3,
        <= 40 => 4,
        <= 60 => 5,
         > 60 => 6
    };
}
public class ActivityWeekVM(IEnumerable<ActivityCellVM> weekCells): ObservableObject
{
    public ObservableCollection<ActivityCellVM> Days { get; } 
        = [..weekCells.OrderBy(d => d.Date)];
}