using AutoMapper;
using FlashMemo.Model.Domain;
using FlashMemo.Model.Persistence;
using FlashMemo.Repositories;
using Microsoft.EntityFrameworkCore;

namespace FlashMemo.Services;
public class CardService(IDbContextFactory<AppDbContext> factory, IMapper mapper): DbDependentClass(factory), ICardService
{
    private readonly IMapper mapper = mapper;
    
    public async Task<CardEntity> ReviewCardAsync(long cardId, ScheduleInfo scheduleInfo, Answers answer, TimeSpan answerTime)
    {
        var db = GetDb;

        var cardEntity = await db.Cards
            .Include(c => c.Deck)
            .SingleAsync(c => c.Id == cardId);

        var domainCard = mapper.Map<Card>(cardEntity);

        domainCard.Review(scheduleInfo);
        
        mapper.Map(domainCard, cardEntity);
        
        var log = CardLog.CreateReviewLog(
            cardEntity, answer, answerTime);
        
        await db.CardLogs.AddAsync(log);
        await db.SaveChangesAsync();

        return cardEntity;
    }

    ///<summary>Updates scalars, note, FKs, and syncs tags.<summary>
    public async Task SaveEditedCard(CardEntity updated, CardAction action, AppDbContext? db = null)
    {
        bool dbProvided = db is not null;
        db ??= GetDb;
        
        var tracked = await db.Cards
            .Include(c => c.Tags)
            .SingleAsync(c => c.Id == updated.Id);

        db.Entry(tracked).CurrentValues
            .SetValues(updated);

        updated.Note.MapTo(tracked.Note);

        SyncTags(tracked, updated, db);
        
        var log = CardLog
            .CreateLog(tracked, action);

        await db.CardLogs.AddAsync(log);

        if (!dbProvided)
            await db.SaveChangesAsync();
    }
    private static void SyncTags(CardEntity tracked, CardEntity updated, AppDbContext db)
    {
        // Sync only diffs on the M:N relationship.
        // Incoming tags can be detached/no-tracking instances, so for adds we must
        // use entities tracked by this DbContext (or attach stubs as Unchanged).

        var incomingTagIds = updated.Tags
            .Select(t => t.Id)
            .ToHashSet();

        var currentTagIds = tracked.Tags
            .Select(t => t.Id)
            .ToHashSet();

        var toRemove = tracked.Tags
            .Where(t => !incomingTagIds.Contains(t.Id))
            .ToList();

        foreach (var tag in toRemove)
            tracked.Tags.Remove(tag);

        var toAddIds = incomingTagIds
            .Where(id => !currentTagIds.Contains(id));

        foreach (var tagId in toAddIds)
        {
            var tag = db.Tags.Local.FirstOrDefault(t => t.Id == tagId)
                ?? db.Attach(new Tag(tagId)).Entity;

            tracked.Tags.Add(tag);
        }
    }
    
    ///<summary>Updates scalars, FKs, and syncs tags.<summary>
    public async Task SaveEditedCards(IEnumerable<CardEntity> updatedCards, CardAction action)
    {
        var db = GetDb;

        foreach (var card in updatedCards)
            await SaveEditedCard(card, action, db);

        await db.SaveChangesAsync();
    }
    
    public async Task UnburyIfNextDay()
    {
        var today = DateTime.Today;

        await GetDb.Cards.Where(c => c.BuriedDate != null 
        && c.BuriedDate.Value.Date < today)
            .ExecuteUpdateAsync(opt => opt
                .SetProperty(c => c.BuriedDate, (DateTime?)null)
                .SetProperty(c => c.IsBuried, false));
    }
}