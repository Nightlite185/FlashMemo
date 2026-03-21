using FlashMemo.Model.Domain;
using FlashMemo.Model.Persistence;

namespace FlashMemo.Repositories;

public interface ICardRepo
{
    Task DeleteCards(IEnumerable<long> cardIds);
    Task AddCard(CardEntity card);
    /// <summary>Includes Deck nav property with it.</summary>
    Task<CardEntity?> GetById(long cardId);
    Task<IEnumerable<CardEntity>> GetByIds(IEnumerable<long> cardIds);
}

public interface IDeckRepo
{
    ///<summary>ONLY UPDATES SCALARS, does not touch navs.</summary>
    Task SaveEditedDeck(Deck updated);
    Task RenameDeck(long id, string name);
    Task<bool> Exists(long id);
    Task AddNewDeck(Deck deck);
    Task RemoveDeck(long deckId);
    Task<Deck> GetFromCard(long cardId);
    Task<Deck?> GetById(long id);

    ///<returns>ILookup with key being the parent deck's Id, and value being IEnumerable of its children decks</returns>
    Task<ILookup<long?, Deck>> ParentIdChildrenLookup(long userId);
    
    ///<returns>first deck from the db. If no decks found -> null</returns>
    Task<Deck?> GetFirst(long userId);
    Task<IDeckMeta> GetDeckMetaById(long deckId);
}

public interface ITagRepo
{
    Task<ICollection<Tag>> GetFromUser(long userId);
    Task<ICollection<Tag>> GetFromCard(long cardId);
    Task CreateNew(Tag newTag);
    Task Remove(long tagId);
    Task SaveEdited(Tag updated);
    Task<Tag> GetById(long tagId);
}

public interface IUserRepo
{
    Task CreateNew(UserEntity toAdd);
    Task<ICollection<UserEntity>> GetAllAsync();
    Task<UserEntity> GetByIdAsync(long userId);
    Task Rename(long userId, string newName);
    Task Remove(long userId);
}