using System.Collections.ObjectModel;
using FlashMemo.Helpers;
using FlashMemo.Services;
using FlashMemo.ViewModel.Wrappers;

namespace FlashMemo.ViewModel.Other;

public partial class ActivityGridVM(IActivityVMBuilder builder, IUserOptionsService userOptService, 
                                    IVMEventBus bus, long userId): BaseVM(bus)
{
    private UserOptions userOpt = null!;
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
    public ObservableCollection<ActivityWeekVM> Weeks 
    { get; private init; } = [];
    public bool HeatmapVisibility => userOpt.ShowHeatmap;
 