using System.Collections.ObjectModel;
using FlashMemo.Helpers;
using FlashMemo.Repositories;
using FlashMemo.ViewModel.Wrappers;
using FlashMemo.Model.Domain;
using AutoMapper;
using FlashMemo.Model.Persistence;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.ComponentModel;

namespace FlashMemo.ViewModel.Windows;

public sealed partial class DeckOptionsMenuVM(IMapper mapper, IDeckOptionsRepo repo, Deck deck): ObservableObject//: OptionsVMBase<DeckOptions>(mapper), IViewModel
{
    #region public properties
    [ObservableProperty]
    public partial DeckOptionsVM OptionsVM { get; set; } = null!;
    public ObservableCollection<DeckOptions> AllPresets { get; init; } = [];
    #endregion

    #region methods
    
    internal async Task InitializeAsync()
    {
        AllPresets.AddRange(
            await deckOptRepo.GetAllFromUser(deck.UserId));

        lastSaved = AllPresets.Single(
            p => p.Id == deck.OptionsId);
        
        OptionsVM = mapper
            .Map<DeckOptionsVM>(lastSaved);
    }
    
    #endregion

    #region private things
    private readonly Deck deck = deck;
    private DeckOptions lastSaved = null!;
    private readonly IDeckOptionsRepo deckOptRepo = repo;
    private static DeckOptions DefaultOptions => DeckOptions.Default;
    #endregion

    #region ICommands
    [RelayCommand]
    private async Task SaveChanges()
    {
        //* this applies any changed made in the preset,
        //* as well as syncs the relation between deck and its preset.

        var saving = mapper.Map<DeckOptions>(OptionsVM);

        await deckOptRepo.SaveEditedPreset(saving);
        await deckOptRepo.AssignToDecks([deck.Id], saving.Id);
    }

    [RelayCommand]
    private async Task DeletePreset()
    {
        // TODO: "are you sure???" pop up show here
        
        await deckOptRepo.Remove(OptionsVM.Id); // if accepted
    }

    private async Task ClonePresetFrom(DeckOptions opt)
    {
        var clone = opt with 
        { 
            Id = IdGetter.Next(), 
            Name = $"{opt.Name} - copy"
        };

        await deckOptRepo.CreateNew(clone);
        
        AllPresets.Add(clone);
    }

    [RelayCommand]
    private void ToDefault()
    {
        mapper.Map(DefaultOptions, OptionsVM);
    }

    [RelayCommand]
    private void RevertChanges()
    {
        mapper.Map(lastSaved, OptionsVM);
    }

    [RelayCommand]
    private void ChangePreset(DeckOptionsVM chosenPreset) //? param placeholder, prob cant have params in a command.
    {
        // TODO: if any not-persisted changes made and this is called -> "are you sure?" pop-up opens.
        
        // TODO: figure out where the preset chosen and clicked from the list even comes from, how can I access it here
        // todo: and intercept it right before changing, to open "are you sure?", and executing some code when user accepts to discard changes.
        
        OptionsVM = chosenPreset;
        deck.OptionsId = chosenPreset.Id;

        lastSaved = mapper.Map<DeckOptions>(chosenPreset);
    }
    #endregion
}
