using FlashMemo.Model;
using FlashMemo.Model.Domain;
using FlashMemo.Model.Persistence;
using FlashMemo.ViewModel.Wrappers;

namespace FlashMemo.Services;

public interface ILoginService
{
    Task ChangeUser(long userId);
}

public interface IDeckTreeBuilder
{
    Task<IEnumerable<DeckNode>> BuildAsync(long userId);
    Task<IEnumerable<DeckNode>> BuildCountedAsync(long userId);
}

public interface ICountingService
{
    ///<returns>an IDictionary, of which keys are deck ids, and values are corresponding CardsCount structs;
    ///containing count of cards grouped by their state.</returns>
    Task<IDictionary<long, CardsCount>> StudyableCards(long userId);
    Task<LessonReviewTake> CalculateTakings(
        long userId, CardsByStateQ cardsQ, DeckOptionsEntity deckOpt);
    Task<int> AllCards(long userId);
    Task<int> AllDecks(long userId);
    Task<int> AllReviewableCards(long userId);
}
public interface ICardQueryService
{
    Task<IEnumerable<CardEntity>> GetCardsWhere(
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
    long? LastUserId { get; set; }
    long? LastDeckId { get; set; }
    Filters? LastFilters { get; set; }

    Task LoadAsync();
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
    Task CreateNew(DeckOptions newRecord);
    Task CreateNew(DeckOptionsEntity newEntity);
    Task SaveEditedPreset(DeckOptions updatedRecord);
    Task AssignToDeck(long deckId, long newPresetId);
}

public interface INoteComparer
{
    bool AreEqual(NoteComparable first, NoteComparable second);
    NoteComparable FromModel(Note savedNote);
    NoteComparable FromEditor(NoteTypes noteType, string frontText, string backText);
}

public interface IActivityVMBuilder
{
    Task<ICollection<ActivityWeekVM>> BuildWeeks(long userId, short year);
}

public interface IClock
{
    DateTime Now { get; }
    DateTime Today { get; }
}