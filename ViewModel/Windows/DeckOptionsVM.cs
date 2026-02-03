using System.Collections.ObjectModel;
using System.Runtime.CompilerServices;
using CommunityToolkit.Mvvm.ComponentModel;
using FlashMemo.Model.Persistence;
using FlashMemo.Helpers;
using FlashMemo.Repositories;
using FlashMemo.ViewModel.Bases;
using FlashMemo.ViewModel.Wrappers;
using FlashMemo.Model.Domain;

namespace FlashMemo.ViewModel.Windows;

public sealed partial class DeckOptionsVM(IDeckOptionsRepo repo): OptionsVMBase<DeckOptionsEntity>, IViewModel
{
    #region public properties
    [ObservableProperty] 
    public partial OrderingOptVM OrderingVM { get; set; }

    [ObservableProperty] 
    public partial DailyLimitsOptVM DailyLimitsVM { get; set; }

    [ObservableProperty] 
    public partial SchedulingOptVM SchedulingVM { get; set; }
    public readonly ObservableCollection<DeckOptionsEntity> allPresets = [];
    #endregion

    #region methods
    
    [Obsolete("to replace with a factory instead")]
    public override async Task Initialize(long userId,
        [CallerMemberName] string? caller = null,
        [CallerFilePath] string? file = null)
    {
        _ = base.Initialize(userId, caller, file);

        allPresets.AddRange(await deckOptRepo.GetAllFromUser(userId));
        
        //TODO: FILL cachedPersisted HERE !!!
        // I probably need more context info in this method like deckId of the deck which Im currently showing options for.
        // SO its prob not the best idea to have it in base.
        // Gotta do sth ab it.
    }
    
    #endregion

    #region private things
    private readonly IDeckOptionsRepo deckOptRepo = repo;
    protected override DeckOptionsEntity DefaultOptions => DeckOptions.Default; // TODO: fix this with the new record type.
    #endregion

    #region ICommands

    #endregion
}
