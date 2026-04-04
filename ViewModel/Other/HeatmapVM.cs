using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FlashMemo.Helpers;
using FlashMemo.Model.Persistence;
using FlashMemo.Services;
using FlashMemo.ViewModel.Bases;
using FlashMemo.ViewModel.Wrappers;

namespace FlashMemo.ViewModel.Other;

public partial class HeatmapVM(IActivityVMBuilder builder, IUserOptionsService userOptService, 
                                    IVMEventBus bus, long userId): BaseVM(bus)
{
    #region private things
    private UserOptions userOpt = null!;
    private bool CanShiftForward => HeatmapYear < DateTime.Today.Year;
    private bool CanShiftBack => HeatmapYear > MinYear;
    private const short MinYear = 2010;
    #endregion

    #region methods
    internal async Task InitAsync()
    {
        eventBus.UserOptionsChanged += OnUserOptChanged;
        eventBus.DomainChanged += OnDomainChanged;

        userOpt = await userOptService
            .GetFromUser(userId);

        await ReloadDomainAsync();
    }
    protected override async Task ReloadDomainAsync()
    {
        if (!HeatmapVisibility)
            return;

        Weeks.Clear();
        
        Weeks.AddRange(await builder
            .BuildWeeks(userId, HeatmapYear));
    }
    protected override async Task ReloadUserOptAsync()
    {
        // caching previous vis (we dont reload if its off
        // to optimize and save resources).
        bool outdated = !HeatmapVisibility;

        // loading fresh options
        userOpt = await userOptService
            .GetFromUser(userId);

        // if previous vis was off (outdated)
        // and now its turned on -> reload heatmap
        if (outdated && HeatmapVisibility)
            await ReloadDomainAsync();

        // notify vis property changed either way
        OnPropertyChanged(nameof(HeatmapVisibility));
    }

    private void NotifyCanShiftChanged()
    {
        ShiftYearBackCommand.NotifyCanExecuteChanged();
        ShiftYearForwardCommand.NotifyCanExecuteChanged();
    }
    #endregion

    #region public properties
    [ObservableProperty]
    public partial short HeatmapYear { get; private set; } = (short)DateTime.Today.Year;
    public ObservableCollection<ActivityWeekVM> Weeks 
    { get; private init; } = [];
    public bool HeatmapVisibility => userOpt.ShowHeatmap;
    #endregion
    
    #region ICommands
    [RelayCommand(CanExecute = nameof(CanShiftBack))]
    private async Task ShiftYearBack()
    {
        if (!CanShiftBack) return;

        Weeks.Clear();

        Weeks.AddRange(await builder
            .BuildWeeks(userId, --HeatmapYear));

        NotifyCanShiftChanged();
    }

    [RelayCommand(CanExecute = nameof(CanShiftForward))]
    private async Task ShiftYearForward()
    {
        if (!CanShiftForward) return;

        Weeks.Clear();
        
        Weeks.AddRange(await builder
            .BuildWeeks(userId, ++HeatmapYear));

        NotifyCanShiftChanged();
    }
    #endregion
}