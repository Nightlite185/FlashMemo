using FlashMemo.Model;
using FlashMemo.Model.Domain;
using FlashMemo.Model.Persistence;
using FlashMemo.ViewModel.Wrappers;

namespace FlashMemo.Services;

public interface ILoginService
{
    void ChangeUser(long userId);
}
public interface IDeckTreeBuilder
{
    Task<IEnumerable<DeckNode>> BuildAsync(long userId);
    Task<IEnumerable<DeckNode>> BuildCountedAsync(long userId);
}

public interface ICardQueryBuilder
{
    Task<IQueryable<CardEntity>> AllCardsInDeckQAsync(long deckId, AppDbContext db);
}

public interface ICountingService
{
    ///<returns>an IDictionary, of which keys are deck ids, and values are corresponding CardsCount structs;
    ///containing count of cards grouped by their state.</returns>
    Task<IDictionary<long, CardsCount>> CardsByState(IEnumerable<long> deckIds, bool onlyForStudy);
    Task<IDictionary<long, CardsCount>> CardsByState(long userId, bool onlyForStudy);
    Task<int> AllCards(long userId);
    Task<int> AllDecks(long userId);
    Task<int> AllReviewableCards(long userId);
}
public interface ICardQueryService
{
    Task<IEnumerable<CardEntity>> GetCardsWhere(Filters filters, CardsOrder order, SortingDirection dir);
    Task<(ICollection<CardEntity>, CardsCount)> GetForStudy(long deckId);
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
    LastSessionData Current { get; }
    void UpdateDeck(long deckId);
    void UpdateUser(long userId);

    Task LoadAsync();
    Task SaveStateAsync();
}

public interface IDeckOptVMBuilder
{
    Task<ICollection<DeckOptionsVM>> BuildAllCounted(long userId);
}