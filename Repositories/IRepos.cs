using FlashMemo.Model.Persistence;

namespace FlashMemo.Repositories;

public interface ICardRepo
{
    public Task DeleteCards(IEnumerable<CardEntity> cards);
    public Task AddCard(CardEntity card);
    public Task<CardEntity> GetCard(long cardId);
}

public interface IDeckRepo
{
    ///<summary>ONLY UPDATES SCALARS, does not touch navs.</summary>
    public Task SaveEditedDeckAsync(Deck updated);
    public Task AddNewDeckAsync(Deck deck);
    public Task DeleteDeckAsync(long deckId);
    public Task<Deck> LoadDeckAsync(long id);
    public Task<ILookup<long?, Deck>> BuildDeckLookupAsync(long userId, AppDbContext? db = null);
}

public interface ITagRepo
{
    public Task<IEnumerable<Tag>> GetFromUserAsync(long userId);
    public Task<IEnumerable<Tag>> GetFromCardAsync(long cardId);
    public Task AddNewAsync(params IEnumerable<Tag> tags);
    public Task RemoveAsync(params IEnumerable<Tag> tags);
    public Task SaveEdited(Tag updated);
}

public interface IUserRepo
{
    public Task<ICollection<UserEntity>> GetAllAsync();
    public Task<UserEntity> GetByIdAsync(long userId);
    public Task Remove(long userId);
    public Task SaveEdited(UserEntity edited);
}

public interface IDeckOptionsRepo
{
    public Task<IEnumerable<DeckOptions>> GetAllFromUser(long userId);
    public Task Remove(long presetId);
    public Task AddNew(DeckOptions options);
    public Task SaveEditedPreset(DeckOptions edited);
}