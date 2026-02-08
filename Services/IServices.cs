using FlashMemo.Model;
using FlashMemo.Model.Domain;
using FlashMemo.Model.Persistence;
using FlashMemo.ViewModel;
using FlashMemo.ViewModel.Wrappers;

namespace FlashMemo.Services;

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
    public Task<IDictionary<long, CardsCount>> CardsByState(IEnumerable<long> deckIds, bool countOnlyStudyable);
    public Task<IDictionary<long, CardsCount>> CardsByState(long userId, bool countOnlyStudyable);
    Task<int> AllCards(long userId);
    Task<int> AllDecks(long userId);
    Task<int> AllReviewableCards(long userId);
}
public interface ICardQueryService
{
    public Task<IEnumerable<CardEntity>> GetCardsWhere(Filters filters, CardsOrder order, SortingDirection dir);
    public Task<IEnumerable<CardEntity>> GetForStudy(long deckId);
    public Task<IList<CardEntity>> GetAllFromUser(long userId);
    public Task<IList<CardEntity>> GetAllFromDeck(long deckId);
}

public interface ICardService
{
    /// <summary>Deck needs to be included in cardEntity argument; otherwise this won't work</summary>
    /// <returns>reviewed card's new state</returns>
    Task<CardState> ReviewCardAsync(long cardId, Answers answer, TimeSpan answerTime);
    
    ///<summary>Updates scalars and syncs tags collection. DOES NOT WORK FOR NAV PROPERTIES LIKE Deck</summary>
    Task SaveEditedCard(CardEntity updated, CardAction action, AppDbContext? db = null);

    Task SaveEditedCards(IEnumerable<CardEntity> updatedCards, CardAction action);
}

public interface IDisplayControl
{
    //void Switch<TViewModel>() where TViewModel : IViewModel;
    Task SwitchToDecks(long userId);
}

[Obsolete("ws is now based on event communication, not direct method calling.")]
public interface IWindowService
{
    // void ShowDialog<TViewModel>() where TViewModel: IViewModel;
    // Task ShowEditCard(long cardId, long userId);
    // void ShowCreateCard(DeckNode targetDeck);
    // Task ShowBrowse(long userId);
    // Task ShowUserSelect();
    // Task ShowUserSettings(long userId);
}

public interface IUserVMBuilder
{
    Task<IEnumerable<UserVM>> BuildAllCounted();
    Task<UserVM> BuildCounted(long userId);
    Task<UserVM> BuildCounted(UserEntity user);
    UserVM BuildUncounted(UserEntity user);
}

public interface ISessionDataService
{
    LastSessionData Current { get; }
    Task LoadAsync();
    Task SaveStateAsync();

}

public interface IDeckOptVMBuilder
{
    Task<ICollection<DeckOptionsVM>> BuildAllCounted(long userId);
}