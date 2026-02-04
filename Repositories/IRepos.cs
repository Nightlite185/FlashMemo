using FlashMemo.Model.Domain;
using FlashMemo.Model.Persistence;

namespace FlashMemo.Repositories;

public interface ICardRepo
{
    Task DeleteCards(IEnumerable<CardEntity> cards);
    Task AddCard(CardEntity card);
    Task<CardEntity> GetCard(long cardId);
}

public interface IDeckRepo
{
    ///<summary>ONLY UPDATES SCALARS, does not touch navs.</summary>
    Task SaveEditedDeckAsync(Deck updated);
    Task AddNewDeckAsync(Deck deck);
    Task DeleteDeckAsync(long deckId);
    Task<Deck> LoadDeckAsync(long id);
    Task<ILookup<long?, Deck>> BuildDeckLookupAsync(long userId, AppDbContext? db = null);
    Task<IEnumerable<long>> GetChildrenIds(long deckId);
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
    Task<IEnumerable<DeckOptions>> GetAllFromUser(long userId);
    Task Remove(long presetId);
    Task CreateNew(DeckOptions newRecord);
    Task SaveEditedPreset(DeckOptions updatedRecord);
    Task AssignToDecks(IEnumerable<long> deckIds, long newPresetId = -1);
}