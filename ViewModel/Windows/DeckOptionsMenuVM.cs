using System.Collections.ObjectModel;
using FlashMemo.Helpers;
using FlashMemo.Repositories;
using FlashMemo.ViewModel.Wrappers;
using FlashMemo.Model.Domain;
using AutoMapper;
using FlashMemo.Model.Persistence;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using FlashMemo.Services;

namespace FlashMemo.ViewModel.Windows;

public sealed partial class DeckOptionsMenuVM(IMapper m, IDeckOptVMBuilder doVMB, IDeckOptionsRepo dor, Deck d): ObservableObject, IViewModel, IClosingAware
{
    #region public properties
    [ObservableProperty] [NotifyPropertyChangedFor(nameof(CanEditSaveDelete))]
    [NotifyPropertyChangedFor(nameof(IsCurrentModified))] [NotifyCanExecuteChangedFor(nameof(SaveChangesCommand))]
    public partial DeckOptionsVM CurrentOptions { get; set; } = null!;
    public ObservableCollection<DeckOptionsVM> AllPresets { get; init; } = [];
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
        get
        {
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
    public bool CanRemovePreset()
    {
        var result = DialogResult.Yes;

        if (CurrentOptions.Decks.Count != 0)
            result = DialogService.Show(
                "Are you sure you want to delete currently viewed preset? Every deck currently referencing this preset, will now be assigned to the default one. Do you wish to proceed?",
                "Are you sure?",
                DialogButtons.YesNo,
                DialogIcons.Warning);

        return result is DialogResult.Yes;
    }
    public bool CanClose() => !IsCurrentModified;
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
            await SaveChanges();

        return result is DialogResult.Yes or DialogResult.No;
        // only cancel clicked doesnt discard it
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
    #endregion

    #region private things
    private readonly Deck deck = d;
    private readonly IMapper mapper = m;
    private readonly IDeckOptVMBuilder vmBuilder = doVMB;
    private readonly IDeckOptionsRepo deckOptRepo = dor;
    private DeckOptions lastSaved = null!;
    #endregion

    #region ICommands
    [RelayCommand(CanExecute = nameof(CanEditSaveDelete))]
    private async Task SaveChanges()
    {
        //* this applies any changed made in the preset,
        //* as well as syncs the relation between deck and its preset.

        ThrowIfDefault(CurrentOptions);

        var saving = mapper.Map<DeckOptions>(CurrentOptions);

        await deckOptRepo.SaveEditedPreset(saving);
        await deckOptRepo.AssignToDecks([deck.Id], saving.Id);

        lastSaved = saving;
    }

    [RelayCommand]
    private async Task RemovePreset()
    {
        ThrowIfDefault(CurrentOptions);

        if (!CanRemovePreset())
            return;

        deck.OptionsId = DeckOptions.DefaultId;

        AllPresets.Remove(CurrentOptions);
        await deckOptRepo.Remove(CurrentOptions.Id);

        CurrentOptions = AllPresets.Single(o => 
            o.Id == DeckOptions.DefaultId);

        lastSaved = mapper.Map<DeckOptions>(CurrentOptions);
    }

    [RelayCommand]
    private async Task ClonePreset()
    {
        var clone = CurrentOptions.CloneWithJson();

        clone.Id = IdGetter.Next();
        clone.Name = $"{CurrentOptions.Name} - copy";

        await deckOptRepo.CreateNew(
            mapper.Map<DeckOptions>(clone));
        
        AllPresets.Add(clone);
    }

    [RelayCommand]
    private void ToDefault()
    {
        mapper.Map(DeckOptions.Default, CurrentOptions);
    }

    [RelayCommand]
    private void RevertChanges()
    {
        mapper.Map(lastSaved, CurrentOptions);
    }

    [RelayCommand]
    private void ChangePreset(DeckOptionsVM chosenPreset)
    {
        //TODO: FIX: when changing presets, there are unsaved changes, I click No (dont save), it needs to revert that VM to its previous state.

        CurrentOptions.AssignedDecksCount--; // decrementing previous preset's deck count
        CurrentOptions = chosenPreset;
        CurrentOptions.AssignedDecksCount++; // incrementing new one's

        deck.OptionsId = chosenPreset.Id;

        lastSaved = mapper.Map<DeckOptions>(chosenPreset);
    }
    #endregion
}