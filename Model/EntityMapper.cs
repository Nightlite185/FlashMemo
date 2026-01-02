using FlashMemo.Model.Domain;
using FlashMemo.Model.Persistence;

namespace FlashMemo.Model
{
    public sealed class MappingCache()
    {
        public readonly Dictionary<long, Tag> tags = [];
        public readonly Dictionary<long, Card> cards = [];
        public readonly Dictionary<long, Deck> decks = [];
        public readonly Dictionary<long, Scheduler> schedulers = [];
        public readonly Dictionary<long, User> users = [];
    }
    public static class EntityMapper
    {
        #region Domain to Entity
        public static CardEntity MapToEntity(this Card card, MappingCache cache)
        {
            
        }

        public static DeckEntity MapToEntity(this Deck deck, MappingCache cache)
        {
            
        }

        public static SchedulerEntity MapToEntity(this Scheduler scheduler, MappingCache cache)
        {
            
        }

        public static UserEntity MapToEntity(this User User, MappingCache cache)
        {
            
        }

        public static TagEntity MapToEntity(this Tag Tag, MappingCache cache)
        {
            
        }
        #endregion

        #region Entity to Domain
        public static Card MapToDomain(this CardEntity ce, MappingCache cache)
        {
            if (cache.cards.TryGetValue(ce.Id, out var cachedCard))
                return cachedCard;

            Card card = new(ce.Id); // db's primary key is generated in the domain when creating a genuinely new card, then mapped to persistence.
            cache.cards.Add(ce.Id, card); // same with Deck, User, etc.
            
            return card.Rehydrate(
                frontContent: ce.FrontContent,
                backContent: ce.BackContent,
                created: ce.Created,
                lastModified: ce.LastModified,
                nextReview: ce.NextReview,
                lastReviewed: ce.LastReviewed,
                interval: ce.Interval,
                state: ce.State,
                learningStage: ce.LearningStage,
                parentDeck: ce.Deck.MapToDomain(cache),
                isBuried: ce.IsBuried,
                isSuspended: ce.IsSuspended,
                tags: [..ce.Tags.Select(t => t.MapToDomain(cache))]
            );
        }
        public static Deck MapToDomain(this DeckEntity de, MappingCache cache)
        {
            if (cache.decks.TryGetValue(de.Id, out var cachedDeck))
                return cachedDeck;

            Deck deck = new(de.Id);
            cache.decks.Add(de.Id, deck);
            
            return deck.Rehydrate(
                cards: [..de.Cards.Select(c => c.MapToDomain(cache))],
                name: de.Name,
                owner: de.Owner.MapToDomain(cache),
                created: de.Created,
                scheduler: de.Scheduler.MapToDomain(cache),
                isTemporary: de.IsTemporary
            );
        }
        public static Scheduler MapToDomain(this SchedulerEntity se, MappingCache cache)
        {
            if (cache.schedulers.TryGetValue(se.Id, out var cachedScheduler))
                return cachedScheduler;

            Scheduler scheduler = new(se.Id);
            cache.schedulers.Add(se.Id, scheduler);

            return scheduler.Rehydrate(
                name: se.Name,
                goodMultiplier: se.GoodMultiplier,
                easyMultiplier: se.EasyMultiplier,
                hardMultiplier: se.HardMultiplier,
                againDayCount: se.AgainDayCount,
                againStageFallback: se.AgainStageFallback,
                goodOnNewStage: se.GoodOnNewStage,
                easyOnNewDayCount: se.EasyOnNewDayCount,
                hardOnNewStage: se.HardOnNewStage,
                learningStages: se.LearningStages.Select(min => TimeSpan.FromMinutes(min))
            );
        }
        public static User MapToDomain(this UserEntity ue, MappingCache cache)
        {
            if (cache.users.TryGetValue(ue.Id, out var cachedUser))
                return cachedUser;

            User user = new(ue.Id);
            cache.users.Add(ue.Id, user);

            return user.Rehydrate(
                username: ue.Username,
                hashedPassword: ue.HashedPassword,
                //cfg: ue.Cfg.MapToDomain(), // not implemented yet
                decks: [..ue.Decks.Select(d => d.MapToDomain(cache))],
                tags: [..ue.Tags.Select(t => t.MapToDomain(cache))],
                schedulers: [..ue.SchedulerPresets.Select(s => s.MapToDomain(cache))]
            );
        }
        public static Tag MapToDomain(this TagEntity te, MappingCache cache)
        {
            if (cache.tags.TryGetValue(te.Id, out var cachedTag))
                return cachedTag;

            Tag tag = new(te.Id);

            cache.tags.Add(tag.Id, tag);
 
            return tag.Rehydrate(
                name: te.Name,
                color: te.Color,
                owner: te.Owner.MapToDomain(cache)
            );
        }
        #endregion
    }
}