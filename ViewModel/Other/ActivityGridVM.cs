using System.Collections.ObjectModel;
using FlashMemo.Helpers;
using FlashMemo.Services;
using FlashMemo.ViewModel.Wrappers;

namespace FlashMemo.ViewModel.Other;

public partial class ActivityGridVM(IActivityVMBuilder builder, long userId)
{
    public ObservableCollection<ActivityWeekVM> Weeks { get; private init; } = [];

    public async Task ReloadAsync()
    {
        Weeks.Clear();
        
        Weeks.AddRange(await builder
            .BuildWeeks(userId));
    }
}