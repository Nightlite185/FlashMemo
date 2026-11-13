using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FlashMemo.Helpers;
using FlashMemo.Model.Persistence;
using FlashMemo.Services;
using FlashMemo.ViewModel.Bases;
using FlashMemo.ViewModel.Wrappers;
using Microsoft.Extensions.Logging;

namespace FlashMemo.ViewModel.Other;

/// <summary>
/// Shows a snapshot of review activity, refreshed on initialization, year/option changes,
/// and domain notifications received while unfocused, when focus returns.
/// </summary>
/// <remarks>
/// BaseVM ignores domain notifications while focused. The current review flow closes
/// DecksVM and its heatmap before reviewing; returning to decks creates a fresh snapshot.
/// If reviews can be saved while this VM stays focused, add a refresh path for that case.
/// </remarks>
public partial class HeatmapVM(IActivityVMBuilder builder, IUserOptionsService userOptService, 
                            IVMEventBus bus, long userId, ILogger<HeatmapVM> logger,
                            IDialogService dialogService, SafetyNet net)
                            : BaseVM(bus, logger, dialogService)
{
    #region private things
    private UserOptions? userOpt;
    private bool isLoading;
    private bool CanShiftForward => Visibility && HeatmapYear < DateTime.Today.Year;
    private bool CanShiftBack => Visibility && HeatmapYear > MinYear;
    private const int MinYear = 2010;
    #endregion

    #region methods
    internal async Task InitAsync()
    {
        eventBus.UserOptionsChanged += OnUserOptChanged;
        eventBus.DomainChanged += OnDomainChanged;

        await ReloadUserOptAsync();
    }
    protected override Task ReloadDomainAsync()
        // BaseVM.OnFocusGained awaits domain reload before checking dirty user options.
        // If both are dirty, its following options reload refreshes the weeks using the new preference.
        => isUserOptDirty ? Task.CompletedTask : LoadAsync(HeatmapYear, reloadOptions: false);

    protected override Task ReloadUserOptAsync()
        => LoadAsync(HeatmapYear, reloadOptions: true);

    private async Task LoadAsync(int year, bool reloadOptions)
    {
        // Error dialogs can return focus while the original load is still in progress.
        if (isLoading || (!reloadOptions && userOpt?.ShowHeatmap != true))
            return;

        isLoading = true;
        IsLoaded = false;

        try
        {
            var result = await net.TryUnsafe(
                action: async () =>
                {
                    if (reloadOptions)
                    {
                        userOpt = null;
                        userOpt = await userOptService.GetFromUser(userId);
                    }

                    return userOpt!.ShowHeatmap
                        ? await builder.BuildWeeks(userId, year)
                        : [];
                },
                actionTried: "load the review heatmap",
                retry: true);

            if (!result.Success)
                return;

            Weeks.Clear();
            Weeks.AddRange(result.Value);
            HeatmapYear = year;
            IsLoaded = userOpt!.ShowHeatmap;
        }
        finally { isLoading = false; }
    }

    #endregion

    #region public properties
    [ObservableProperty]
    public partial int HeatmapYear { get; private set; } = DateTime.Today.Year;
    public ObservableCollection<ActivityWeekVM> Weeks 
    { get; private init; } = [];

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Visibility))]
    [NotifyCanExecuteChangedFor(nameof(ShiftYearBackCommand), nameof(ShiftYearForwardCommand))]
    public partial bool IsLoaded { get; private set; }

    public bool Visibility => userOpt?.ShowHeatmap == true && IsLoaded;
    #endregion
    
    #region ICommands
    [RelayCommand(CanExecute = nameof(CanShiftBack))]
    private async Task ShiftYearBack()
    {
        if (!CanShiftBack) return;

        await LoadAsync(HeatmapYear - 1, reloadOptions: false);
    }

    [RelayCommand(CanExecute = nameof(CanShiftForward))]
    private async Task ShiftYearForward()
    {
        if (!CanShiftForward) return;

        await LoadAsync(HeatmapYear + 1, reloadOptions: false);
    }
    #endregion
}