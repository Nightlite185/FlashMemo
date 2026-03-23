using CommunityToolkit.Mvvm.ComponentModel;
using FlashMemo.ViewModel.Bases;

namespace FlashMemo.ViewModel.Popups;

public partial class RescheduleVM
    (Func<IRescheduleData, Task> confirm, Action cancel)
    : PopupVMBase(cancel)
{
    private readonly Func<IRescheduleData, Task> confirm = confirm;
    protected override async Task Confirm()
    {
        var minDate = MinPickableDate;

        if (DatepickerActive)
        {
            var dt = (RescheduleToDate.Date < minDate)
                ? minDate
                : RescheduleToDate.Date;

            await confirm(new RescheduleData(dt, KeepInterval));
        }

        else
        {
            await confirm(new PostponeData(
                PostponeByDays, SinceToday, 
                KeepInterval));
        }

        Close();
    }

    public DateTime MinPickableDate => DateTime.Today.AddDays(1).Date;

    [ObservableProperty] public partial bool DatepickerActive { get; set; } = false;
    [ObservableProperty] public partial DateTime RescheduleToDate { get; set; } = 
        DateTime.Today.AddDays(1).Date;

    [ObservableProperty] public partial uint PostponeByDays { get; set; } = 1;
    [ObservableProperty] public partial bool SinceToday { get; set; } = false; 
    // Controls whether the card's due date gets postponed in time by x days *since today* OR *since its current due date*.

    [ObservableProperty] public partial bool KeepInterval { get; set; } = false;

    partial void OnRescheduleToDateChanged(DateTime value)
    {
        if (!DatepickerActive)
            return;

        var minPickableDate = MinPickableDate;
        if (value.Date < minPickableDate)
            RescheduleToDate = minPickableDate;
    }
    partial void OnPostponeByDaysChanged(uint value)
    {
        if (value == 0)
            PostponeByDays = 1;
    }
    partial void OnDatepickerActiveChanged(bool value)
    {
        if (!value)
            return;

        if (RescheduleToDate.Date < MinPickableDate)
            RescheduleToDate = MinPickableDate;
    }
}

public record struct RescheduleData(
    DateTime NewDate, bool KeepInterval)
    : IRescheduleData;

public record struct PostponeData(
    uint PostponeByDays, bool SinceToday, 
    bool KeepInterval): IRescheduleData;

public interface IRescheduleData 
{ 
    bool KeepInterval { get; } 
};