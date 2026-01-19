using AutoMapper;
using FlashMemo.Model.Domain;
using FlashMemo.Model.Persistence;
using FlashMemo.Repositories;
using Microsoft.EntityFrameworkCore;

namespace FlashMemo.Services
{
    public class CardService(IDbContextFactory<AppDbContext> factory, IMapper mapper): DbDependentClass(factory)
    {
        private readonly IMapper mapper = mapper;
        
        /// <summary>Deck needs to be included in cardEntity argument; otherwise this won't work</summary>
        /// <returns>reviewed card's new state</returns>
        public async Task<CardState> ReviewCardAsync(long cardId, Answers answer, TimeSpan answerTime)
        {
            var db = GetDb;

            var cardEntity = await db.Cards
                .Include(c => c.Deck)
                .SingleAsync(c => c.Id == cardId);

            ArgumentNullException.ThrowIfNull(cardEntity.Deck, nameof(cardEntity.Deck));

            var domainCard = mapper.Map<Card>(cardEntity);
            var options = cardEntity.Deck.Options.Scheduling;

            domainCard.Schedule(answer, options);
            
            var updatedCard = mapper.Map<CardEntity>(domainCard);
            
            db.Entry(cardEntity)
                .CurrentValues
                .SetValues(updatedCard);

            var log = CardLog.CreateReviewLog(
                cardEntity, answer, answerTime);
            
            await db.CardLogs.AddAsync(log);
            await db.SaveChangesAsync();
            
            return domainCard.State;
        }
        
        ///<summary>Updates scalars and syncs tags collection. DOES NOT WORK FOR NAV PROPERTIES LIKE Deck</summary>
        public async Task SaveEditedCard(CardEntity updated, CardAction action, AppDbContext? db = null)
        {
            bool dbProvided = db is not null;
            db ??= GetDb;
            
            var tracked = await db.Cards
                .SingleAsync(c => c.Id == updated.Id);

            db.Entry(tracked).CurrentValues
                .SetValues(updated);

            tracked.SyncTagsFrom(updated);
            
            var log = CardLog
                .CreateLog(tracked, action);

            await db.CardLogs.AddAsync(log);

            if (!dbProvided) 
                await db.SaveChangesAsync();
        }
        public async Task SaveEditedCards(IEnumerable<CardEntity> updatedCards, CardAction action)
        {
            var db = GetDb;

            foreach (var card in updatedCards)
                await SaveEditedCard(card, action, db);

            await db.SaveChangesAsync();
        }
    }
}