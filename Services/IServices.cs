using FlashMemo.Model;
using FlashMemo.Model.Domain;
using FlashMemo.Model.Persistence;
using FlashMemo.ViewModel.Other;
using FlashMemo.ViewModel.Popups;
using FlashMemo.ViewModel.Windows;
using FlashMemo.ViewModel.Wrappers;

namespace FlashMemo.Services;

public interface ILoginService
{
    Task<bool> ChangeUser(long userId);
}

public interface IDeckTreeBuilder
{
    Task<IList<DeckNode>> BuildAsync(long userId);
    Task<IList<DeckNode>> BuildCountedAsync(long userId);
}

public interface ICountingService
{
    ///<returns>an IDictionary, of which keys are deck ids, and values are corresponding CardsCount structs;
    ///containing count of cards grouped by their state.</returns>
    Task<IDictionary<long, CardsCount>> StudyableCards(long userId);
    
    Task<LessonReviewTake> CalculateTakings(
        long userId, CardsByStateQ cardsQ, DeckOptionsEntity deckOpt);

    Task<int> AllCards(long userId);

    /// <summary>Counts all cards in the selected decks and their descendants, without counting overlaps twice.</summary>
    Task<int> AllCardsInDecks(IEnumerable<long> deckIds);
    Task<int> AllDecks(long userId);
    Task<int> AllReviewableCards(long userId);
}
public interface ICardQueryService
{
    Task<IList<CardEntity>> GetCardsWhere(
        Filters filters, CardsOrder order, SortingDirection dir);

    Task<(ICollection<CardEntity>, CardsCount)> GetForStudy(long deckId, long userId);
    Task<IList<CardEntity>> GetAllFromUser(long userId);
    Task<IList<CardEntity>> GetAllFromDeck(long deckId);
    
}

public interface ICardService
{
    ///<returns>the updated card after review</returns>
    Task<CardEntity> ReviewCardAsync(long cardId, ScheduleInfo scheduleInfo, Answers answer, TimeSpan answerTime);
    
    ///<summary>Updates scalars and syncs tags collection. DOES NOT WORK FOR NAV PROPERTIES LIKE Deck</summary>
    Task SaveEditedCard(CardEntity updated, CardAction action, AppDbContext? db = null);
    Task SaveEditedCards(IEnumerable<CardEntity> updatedCards, CardAction action);

    ///<summary>Unburies all cards that were buried at least 1 day ago.</summary>
    Task UnburyIfNextDay();
}

public interface IDisplayControl
{
    Task SwitchToDecks(long userId);
    Task SwitchToStats(long userId);
    Task SwitchToReview(long userId, IDeckMeta deck);
}

public interface IUserVMBuilder
{
    Task<IEnumerable<UserVM>> BuildAllCounted();
    Task<UserVM> BuildCounted(long userId);
    Task<UserVM> BuildCounted(UserEntity user);
    UserVM BuildUncounted(UserEntity user);
}

public interface ILastSessionService
{
    long? LastUserId { get; }
    long? LastDeckId { get; set; }
    Filters? LastFilters { get; set; }

    Task LoadAppSessionAsync();
    Task LoadUserCacheAsync(long userId);
    Task SetLastUserAsync(long userId);
    Task ClearLastUserAsync();
    Task SaveStateAsync();
}

public interface IDeckOptVMBuilder
{
    Task<ICollection<DeckOptionsVM>> BuildAllCounted(long userId);
}

public interface IVMEventBus
{
    event Action? DomainChanged;
    event Action? UserOptionsChanged;
    event Action? DeckOptionsChanged;

    void NotifyDomain();
    void NotifyUserOpt();
    void NotifyDeckOpt();
}

public interface IUserOptionsService
{
    Task Update(long userId, UserOptions updated);
    Task<UserOptions> GetFromUser(long userId);
    Task<byte> GetDayStartOffset(long userId);
}

public interface IDeckOptionsService
{
    Task<DeckOptions> GetFromDeck(long deckId);
    Task<IEnumerable<DeckOptions>> GetAllFromUser(long userId);
    Task<IDictionary<long, DeckOptionsEntity>> MappedByDeckId(long userId);
    Task Remove(long presetId);
    Task Rename(string name, long id);
    Task<DeckOptions> CreateNew(DeckOptions newRecord);
    Task<DeckOptionsEntity> CreateNew(DeckOptionsEntity newEntity);
    Task SaveEditedPreset(DeckOptions updatedRecord);
    Task AssignToDeck(long deckId, long newPresetId);
}

public interface INoteComparer
{
    bool AreEqual(NoteComparable first, NoteComparable second);
    NoteComparable FromModel(Note savedNote);
    NoteComparable FromEditor(NoteTypes noteType, string frontContent, string backContent);
}

public interface IActivityVMBuilder
{
     Task<ICollection<ActivityWeekVM>> BuildWeeks(long userId, int year);
}

public interface IStatsQueryService
{
    Task<int> GetAnswerRatio(Answers answer, TimePeriod lastPeriod, long userId);
    Task<DayOfWeek?> DayWithMostReviewsInLastMonth(long userId);
    Task<TimeSpan> AvgAnswerTimeInLastMonth(long userId);
    Task<int> MostReviewedHourOfDayInLastMonth(long userId);
    Task<int> TotalReviewsEver(long userId);
    Task<int> LongestReviewStreak(long userId);
    Task<int> CurrentReviewStreak(long userId);
}

public interface IMainWindowBootstrapper
{
    bool EnsureReadyForSwitch();
    void ActivateMainVM(MainVM vm);
    void Shutdown();
}

public interface IDialogService
{
    DialogResult Show(string title, string message, DialogButtons buttons, DialogIcons icon);
    DialogResult ShowWarning(string message, string title = "Warning", DialogButtons buttons = DialogButtons.OK);
    DialogResult ShowError(string message, string title = "Error", DialogButtons buttons = DialogButtons.OK);
    DialogResult ShowInfo(string message, string title = "Info", DialogButtons buttons = DialogButtons.OK);
    void ShowNoCardsFoundToExport();
}

public interface IExportService
{
    string PrepareTxt(IEnumerable<CardEntity> cards, string fieldSeparator, char noteSeparator);
    string PrepareCsv(IEnumerable<CardEntity> cards,
                    IReadOnlyDictionary<long, string> deckPaths,
                    IEnumerable<CsvExportColumn> columns);
    Task SaveAsync(string content, string path);
}

public interface IImportService
{
    Task<bool> ImportTxtAsync(string path, string fieldSep, NoteSeparator noteSep, IDeckMeta deck);
    Task<bool> ImportCsvAsync(string path, long deckId);
    Task<bool> ImportFlameAsync(string path, long deckId);
}