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

public sealed partial class DeckOptionsMenuVM(IMapper m, IDeckOptVMBuilder doVMB, IDeckOptionsRepo dor, Deck d): ObservableObject, IViewModel
{
    #region public properties
    [ObservableProperty] [NotifyPropertyChangedFor(nameof(CanSaveOrDelete))]
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

    public bool CanSaveOrDelete => CurrentOptions.Id != DeckOptions.DefaultId;
    public bool IsCurrentModified {
        get
        {
            if (CurrentOptions.Id != lastSaved.Id)
                throw new InvalidOperationException(
                "CurrentOptions's id must match with lastSaved's id");

            var snapshot = mapper
                .Map<DeckOptions>(CurrentOptions);

            return !snapshot.Equals(lastSaved);
        }
    }
    public bool RemoveRequiresConfirmation => CurrentOptions.Decks.Count != 0;
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
    [RelayCommand(CanExecute = nameof(CanSaveOrDelete))]
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
        CurrentOptions.AssignedDecksCount--; // decrementing previous preset's deck count
        CurrentOptions = chosenPreset;
        CurrentOptions.AssignedDecksCount++; // incrementing new one's

        deck.OptionsId = chosenPreset.Id;

        lastSaved = mapper.Map<DeckOptions>(chosenPreset);
    }
    #endregion
}