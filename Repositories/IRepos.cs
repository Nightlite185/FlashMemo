using FlashMemo.Model.Domain;
using FlashMemo.Model.Persistence;

namespace FlashMemo.Repositories;

public interface ICardRepo
{
    Task DeleteCards(IEnumerable<CardEntity> cards);
    Task AddCard(CardEntity card);
    Task<CardEntity> GetById(long cardId);
}

public interface IDeckRepo
{
    ///<summary>ONLY UPDATES SCALARS, does not touch navs.</summary>
    Task SaveEditedDeck(Deck updated);
    Task AddNewDeck(Deck deck);
    Task RemoveDeck(long deckId);
    Task<Deck> GetById(long id);

    ///<returns>ILookup with key being the parent deck's Id, and value being IEnumerable of its children decks</returns>
    Task<ILookup<long?, Deck>> ParentIdChildrenLookup(long userId, AppDbContext? db = null);
    Task<IEnumerable<long>> GetChildrenIds(long deckId);
    
    ///<returns>first deck from the db. If no decks found -> null</returns>
    Task<IDeckMeta?> GetFirstDeckMeta(long userId);
    Task<IDeckMeta> GetDeckMetaById(long deckId);
}

public interface ITagRepo
{
    Task<IEnumerable<Tag>> GetFromUser(long userId);
    Task<IEnumerable<Tag>> GetFromCard(long cardId);
    Task AddNew(params IEnumerable<Tag> tags);
    Task Remove(params IEnumerable<Tag> tags);
    Task SaveEdited(Tag updated);
    Task<IEnumerable<Tag>> GetByIds(IEnumerable<long> tagIds);
}

public interface IUserOptionsRepo
{
    
}

public interface IUserRepo
{
    Task CreateNew(UserEntity toAdd);
    Task<ICollection<UserEntity>> GetAllAsync();
    Task<UserEntity> GetByIdAsync(long userId);
    Task Rename(long userId, string newName);
    Task Remove(long userId);
}

public interface IDeckOptionsRepo
{
    Task<DeckOptions> GetFromDeck(long deckId);
    Task<IEnumerable<DeckOptions>> GetAllFromUser(long userId);
    Task Remove(long presetId);
    Task CreateNew(DeckOptions newRecord);
    Task CreateNew(DeckOptionsEntity newEntity);
    Task SaveEditedPreset(DeckOptions updatedRecord);
    Task AssignToDecks(IEnumerable<long> deckIds, long newPresetId = -1);
}