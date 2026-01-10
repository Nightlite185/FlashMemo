using AutoMapper;
using FlashMemo.Model;
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
        public async Task ReviewCardAsync(long cardId, Answers answer, TimeSpan answerTime)
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
        }
        public async Task SaveEditedCard(CardEntity editedCard, CardAction action)
        {
            var db = GetDb;

            var trackedCard = await db.Cards
                .SingleAsync(c => c.Id == editedCard.Id);

            db.Entry(trackedCard).CurrentValues
                .SetValues(editedCard);
            
            var log = CardLog
                .CreateLog(trackedCard, action);
                
            await db.SaveChangesAsync();
        }
    }
}