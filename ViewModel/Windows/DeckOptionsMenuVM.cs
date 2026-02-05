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
    [ObservableProperty]
    public partial DeckOptionsVM CurrentOptions { get; set; } = null!;
    public ObservableCollection<DeckOptionsVM> AllPresets { get; init; } = [];
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
    
    #endregion

    #region private things
    private readonly Deck deck = d;
    private readonly IMapper mapper = m;
    private readonly IDeckOptVMBuilder vmBuilder = doVMB;
    private DeckOptions lastSaved = null!;
    private readonly IDeckOptionsRepo deckOptRepo = dor;
    private static DeckOptions DefaultOptions => DeckOptions.Default;
    #endregion

    #region ICommands
    [RelayCommand]
    private async Task SaveChanges()
    {
        //* this applies any changed made in the preset,
        //* as well as syncs the relation between deck and its preset.

        var saving = mapper.Map<DeckOptions>(CurrentOptions);

        await deckOptRepo.SaveEditedPreset(saving);
        await deckOptRepo.AssignToDecks([deck.Id], saving.Id);
    }

    [RelayCommand]
    private async Task DeleteCurrentPreset()
    {
        // TODO: "are you sure???" pop up show here
        
        await deckOptRepo.Remove(CurrentOptions.Id);
        AllPresets.Remove(CurrentOptions);

        // fallback to default when deleting currently viewed preset.
        mapper.Map(DefaultOptions, CurrentOptions);
        deck.OptionsId = -1;
    }

    [RelayCommand]
    private async Task DeletePresetFromList(DeckOptionsVM opt)
    {
        if (opt.Id == CurrentOptions.Id)
        {
            await DeleteCurrentPreset();
            return;
        }
        
        AllPresets.Remove(opt);
        await deckOptRepo.Remove(opt.Id);
    }

    [RelayCommand]
    private async Task ClonePresetFrom(DeckOptionsVM opt)
    {
        var clone = mapper.Map<DeckOptionsVM>(opt);

        clone.Id = IdGetter.Next();
        clone.Name = $"{opt.Name} - copy";

        await deckOptRepo.CreateNew(mapper.Map<DeckOptions>(clone));
        // todo: maybe later add mapping profile directly between vm and entity so no need for double mapping.
        
        AllPresets.Add(clone);
    }

    [RelayCommand]
    private void ToDefault()
    {
        mapper.Map(DefaultOptions, CurrentOptions);
    }

    [RelayCommand]
    private void RevertChanges()
    {
        mapper.Map(lastSaved, CurrentOptions);
    }

    [RelayCommand]
    private void ChangePreset(DeckOptionsVM chosenPreset)
    {
        // TODO: if any not-persisted changes made and this is called -> "are you sure?" pop-up opens.
        //* DeckOptVM on which the ctx menu was opened goes here as "CommandParam" or whatever its called.
        
        CurrentOptions.DecksAssigned--; // decrementing previous preset's deck count
        CurrentOptions = chosenPreset;
        CurrentOptions.DecksAssigned++; // incrementing new one's

        deck.OptionsId = chosenPreset.Id;

        mapper.Map(chosenPreset, lastSaved);
    }
    #endregion
}
