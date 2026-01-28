using FlashMemo.Model;
using FlashMemo.Model.Domain;
using FlashMemo.Model.Persistence;
using FlashMemo.ViewModel;
using FlashMemo.ViewModel.WrapperVMs;

namespace FlashMemo.Services;

public interface ICardQueryService
{
    public Task<IEnumerable<CardEntity>> GetCardsWhere(Filters filters, CardsOrder order, SortingDirection dir);
    public Task<IEnumerable<CardEntity>> GetForStudy(long deckId);
    public Task<IList<CardEntity>> GetAllFromUser(long userId);
    public Task<IList<CardEntity>> GetAllFromDeck(long deckId);

    ///<returns>an IDictionary, of which keys are deck ids, and values are corresponding CardsCount structs;
    ///containing count of cards grouped by their state.</returns>
    public Task<IDictionary<long, CardsCount>> CountCardsAsync(IEnumerable<long> deckIds, bool countOnlyStudyable);
    public Task<IDictionary<long, CardsCount>> CountCardsAsync(long userId, bool countOnlyStudyable);
}

public interface ICardService
{
    /// <summary>Deck needs to be included in cardEntity argument; otherwise this won't work</summary>
    /// <returns>reviewed card's new state</returns>
    public Task<CardState> ReviewCardAsync(long cardId, Answers answer, TimeSpan answerTime);
    
    ///<summary>Updates scalars and syncs tags collection. DOES NOT WORK FOR NAV PROPERTIES LIKE Deck</summary>
    public Task SaveEditedCard(CardEntity updated, CardAction action, AppDbContext? db = null);

    public Task SaveEditedCards(IEnumerable<CardEntity> updatedCards, CardAction action);
}

public interface INavigationService
{
    public void NavigateTo<TViewModel>() where TViewModel : IViewModel;
}

public interface IWindowService
{
    public void ShowDialog<TViewModel>() where TViewModel: IViewModel;
}

public interface IUserVMBuilder
{
    public Task<IEnumerable<UserVM>> BuildAllAsync();
    public Task<UserVM> BuildByIdAsync(long userId);
}