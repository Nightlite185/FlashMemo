using CommunityToolkit.Mvvm.ComponentModel;
using FlashMemo.ViewModel.Bases;

namespace FlashMemo.ViewModel.Popups;

public partial class RescheduleVM
    (Func<DateTime, bool, Task> confirm, Action cancel)
    : PopupVMBase(cancel)
{
    private readonly Func<DateTime, bool, Task> confirm = confirm;
    protected override async Task Confirm()
    {
        var minDate = MinPickableDate;

        var dt = DatepickerActive
            ? (RescheduleToDate.Date < minDate
                ? minDate
                : RescheduleToDate.Date)
            : DateTime.Today.AddDays(PostponeByDays).Date;

        await confirm(dt, KeepInterval);
        Close();
    }

    // TODO: make so you can choose if postpone by days SINCE today or SINCE card's DUE DATE.
    //! For now its only postponing since today!

    public DateTime MinPickableDate => DateTime.Today.AddDays(1).Date;

    [ObservableProperty] public partial bool DatepickerActive { get; set; } = true;
    [ObservableProperty] public partial DateTime RescheduleToDate { get; set; } = DateTime.Today.AddDays(1).Date;
    [ObservableProperty] public partial uint PostponeByDays { get; set; } = 1;
    [ObservableProperty] public partial bool KeepInterval { get; set; }

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