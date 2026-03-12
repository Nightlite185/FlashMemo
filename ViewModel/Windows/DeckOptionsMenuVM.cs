using System.Collections.ObjectModel;
using FlashMemo.Helpers;
using FlashMemo.ViewModel.Wrappers;
using FlashMemo.Model.Domain;
using AutoMapper;
using FlashMemo.Model.Persistence;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using FlashMemo.Services;
using System.Text.Json;
using System.DirectoryServices.ActiveDirectory;

namespace FlashMemo.ViewModel.Windows;

public sealed partial class DeckOptionsMenuVM(
    IMapper m, IDeckOptVMBuilder doVMB, IDeckOptionsService dor, Deck d, IVMEventBus bus)
    : ObservableObject, IViewModel, IClosingAware, ICloseRequest
{
    #region public properties
    [ObservableProperty] [NotifyPropertyChangedFor(nameof(IsCurrentModified), nameof(CanEditSaveDelete))]
    [NotifyCanExecuteChangedFor(nameof(SaveChangesCommand), nameof(RevertChangesCommand),
    nameof(BeginRenameCurrentPresetCommand), nameof(ToDefaultCommand), nameof(RemovePresetCommand))]
    public partial DeckOptionsVM CurrentOptions { get; set; } = null!;
    [ObservableProperty] public partial bool IsCreatingPreset { get; set; }
    [ObservableProperty] public partial string? NewPresetName { get; set; }
    public ObservableCollection<DeckOptionsVM> AllPresets { get; init; } = [];
    public event Action? OnCloseRequest;
    public IEnumerable<LessonOrder> LessonOrderEnum =>
        Enum.GetValues<LessonOrder>();

    public IEnumerable<ReviewOrder> ReviewOrderEnum =>
        Enum.GetValues<ReviewOrder>();

    public IEnumerable<CardStateOrder> CardStateOrderEnum =>
        Enum.GetValues<CardStateOrder>();

    public IEnumerable<SortingDirection> SortingDirectionEnums =>
        Enum.GetValues<SortingDirection>();

    public bool CanEditSaveDelete => CurrentOptions.Id != DeckOptions.DefaultId;
    public bool IsCurrentModified {
        get {
            if (CurrentOptions.Id == DeckOptions.DefaultId)
                return false;

            if (CurrentOptions.Id != lastSaved.Id)
                throw new InvalidOperationException(
                "CurrentOptions's id must match with lastSaved's id");

            var snapshot = mapper
                .Map<DeckOptions>(CurrentOptions);

            return !snapshot.Equals(lastSaved);
        }
    }
    #endregion

    #region methods
    internal async Task InitializeAsync()
    {
        AllPresets.AddRange(
            await vmBuilder.BuildAllCounted(deck.UserId));

        CurrentOptions = AllPresets.Single(
            p => p.Id == deck.OptionsId);
        
        lastSaved = mapper
            .Map<DeckOptions>(CurrentOptions);
    }
    private static void ThrowIfDefault(DeckOptionsVM opt)
    {
        if (opt.Id == DeckOptions.DefaultId) throw new InvalidOperationException(
            "Removing or editing the default preset is forbidden.");
    }
    public bool CanRemovePreset()
    {
        var result = DialogResult.Yes;

        if (CurrentOptions.DeckCount != 0)
            result = DialogService.Show(
                "Are you sure you want to delete currently viewed preset? Every deck currently referencing this preset, will now be assigned to the default one. Do you wish to proceed?",
                "Are you sure?",
                DialogButtons.YesNo,
                DialogIcons.Warning);

        return result is DialogResult.Yes;
    }
    public async Task<bool> CanCloseAsync() => await CanDiscardAsync();
    public async Task<bool> CanDiscardAsync()
    {
        if (!IsCurrentModified) return true;
        
        var result = DialogService.Show(
            title: "Unsaved changes",
            message: "You have unsaved changes. Do you want to save them?",
            buttons: DialogButtons.YesNoCancel,
            icon: DialogIcons.Warning
        );

        if (result is DialogResult.Yes)
            await SaveCore();

        else if (result is DialogResult.No)
            mapper.Map(lastSaved, CurrentOptions);

        // only cancel clicked doesnt discard it
        return result is DialogResult.Yes or DialogResult.No;
    }
    private async Task SaveCore()
    {
        //* this applies any changed made in the preset,
        //* as well as syncs the relation between deck and its preset.

        ThrowIfDefault(CurrentOptions);

        var saving = mapper.Map<DeckOptions>(CurrentOptions);

        await deckOptService.SaveEditedPreset(saving);
        
        await deckOptService.AssignToDeck(
            deckId: deck.Id,
            newPresetId: saving.Id);

        lastSaved = saving;
        eventBus.NotifyDeckOpt();
    }
    #endregion

    #region private things
    private readonly Deck deck = d;
    private readonly IMapper mapper = m;
    private readonly IDeckOptVMBuilder vmBuilder = doVMB;
    private readonly IDeckOptionsService deckOptService = dor;
    private DeckOptions lastSaved = null!;
    private readonly IVMEventBus eventBus = bus;
    #endregion

    #region ICommands
    [RelayCommand(CanExecute = nameof(CanEditSaveDelete))]
    private async Task SaveChanges()
    {
        await SaveCore();
        OnCloseRequest?.Invoke();
    }

    [RelayCommand(CanExecute = nameof(CanEditSaveDelete))]
    private async Task RemovePreset()
    {
        ThrowIfDefault(CurrentOptions);

        if (!CanRemovePreset())
            return;

        deck.OptionsId = DeckOptions.DefaultId;

        AllPresets.Remove(CurrentOptions);
        await deckOptService.Remove(CurrentOptions.Id);

        CurrentOptions = AllPresets.Single(o => 
            o.Id == DeckOptions.DefaultId);

        lastSaved = mapper.Map<DeckOptions>(CurrentOptions);
        eventBus.NotifyDeckOpt();
    }

    [RelayCommand]
    private async Task ClonePreset()
    {
        //TODO: ask for discarding permission, think if its better to clone unsaved copy or only persisted one.

        const string Name = "Name";
        const string Id = "Id";

        var domainClone = mapper.Map<DeckOptions>(CurrentOptions, opt =>
        {
            opt.Items[Id] = IdGetter.Next();
            opt.Items[Name] = $"{CurrentOptions.Name} - copy";
        });

        await deckOptService.CreateNew(domainClone);
        
        AllPresets.Add(
            mapper.Map<DeckOptionsVM>(domainClone));
    }

    [RelayCommand]
    private async Task AddNewPreset()
    {
        if (string.IsNullOrWhiteSpace(NewPresetName))
            throw new InvalidOperationException(
            "Can't create a new preset with name being null or whitespace.");

        var newPreset = DeckOptions
            .CreateNew(NewPresetName, deck.UserId);

        await deckOptService.CreateNew(newPreset);
        
        AllPresets.Add(
            mapper.Map<DeckOptionsVM>(newPreset));
    }

    [RelayCommand]
    private void BeginCreatePreset()
    {
        CurrentOptions.CancelRenameCommand.Execute(null);
        IsCreatingPreset = true;
        NewPresetName = string.Empty;
    }

    [RelayCommand]
    private void CancelCreatePreset()
    {
        IsCreatingPreset = false;
        NewPresetName = null;
    }

    [RelayCommand(CanExecute = nameof(CanEditSaveDelete))]
    private void BeginRenameCurrentPreset()
    {
        CancelCreatePreset();
        CurrentOptions.BeginRenameCommand.Execute(null);
    }

    [RelayCommand(CanExecute = nameof(CanEditSaveDelete))]
    private void ToDefault()
    {
        mapper.Map(DeckOptions.Default, CurrentOptions);
    }

    [RelayCommand(CanExecute = nameof(CanEditSaveDelete))]
    private void RevertChanges()
    {
        mapper.Map(lastSaved, CurrentOptions);
    }

    [RelayCommand]
    private void ChangePreset(DeckOptionsVM chosenPreset)
    {
        CurrentOptions.DeckCount--; // decrementing previous preset's deck count
        chosenPreset.DeckCount++; // incrementing new one's
        
        CurrentOptions = chosenPreset;
        deck.OptionsId = chosenPreset.Id;

        if (AllPresets.Any(p => p.DeckCount < 0))
            throw new InvalidOperationException(
            "Presets cannot have deck count lower than 0");

        if (CurrentOptions.DeckCount < 1)
            throw new InvalidOperationException(
            $"Current preset needs to have deck count of at least 1, but it was {CurrentOptions.DeckCount}.");

        lastSaved = mapper.Map<DeckOptions>(chosenPreset);
    }

    [RelayCommand(CanExecute = nameof(CanEditSaveDelete))]
    private async Task RenamePreset(DeckOptionsVM options)
    {
        options.CommitRename();
        
        await deckOptService.Rename(
            options.Name, options.Id);
    }
    #endregion
}