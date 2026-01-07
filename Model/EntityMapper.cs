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
        public static void MapToEntity(this Card card, CardEntity entity)
        {
            entity.Id = card.Id;
            entity.FrontContent = card.FrontContent;
            entity.BackContent = card.BackContent;
            entity.Created = card.Created;
            entity.LastModified = card.LastModified;
            entity.NextReview = card.NextReview;
            entity.LastReviewed = card.LastReviewed;
            entity.Interval = card.Interval;
            entity.State = card.State;
            entity.LearningStage = card.LearningStage;
            entity.IsBuried = card.IsBuried;
            entity.IsSuspended = card.IsSuspended;

            card.ParentDeck.MapToEntity(entity.Deck);
            //entity.Tags = [..card.Tags.Select(t => t.MapToEntity())];
            //card.SyncTags(entity);
        }
        public static void MapToEntity(this Deck deck, DeckEntity entity)
        {
            entity.Id = deck.Id;
            entity.Name = deck.Name;
            entity.Created = deck.Created;
            entity.IsTemporary = deck.IsTemporary;

            SyncCards(deck, entity);

            deck.Scheduler.MapToEntity(entity.Scheduler);
            deck.Owner.MapToEntity(entity.User);
            deck.ChildrenDecks.ForEach(d => d.MapToEntity(entity));
        }
        private static void SyncCards(this Deck deck, DeckEntity entity)
        {
            var domainIds = deck
                .Select(c => c.Id)
                .ToHashSet();

            var entityCardsById = entity.Cards
                .ToDictionary(c => c.Id);

            foreach (var domainCard in deck)
            {
                if (entityCardsById.TryGetValue(domainCard.Id, out var entityCard))
                    domainCard.MapToEntity(entityCard); // UPDATE existing tracked entity

                else
                {
                    // ADD new entity
                    var newEntity = new CardEntity();
                    domainCard.MapToEntity(newEntity);
                    entity.Cards.Add(newEntity);
                }
            }

            foreach (var entityCard in entity.Cards)
            {
                if (!domainIds.Contains(entityCard.Id))
                    entity.Cards.Remove(entityCard);
            }
        }

        // private static void SyncTags(this Card card, CardEntity entity)
        // {
            
        // }
        public static void MapToEntity(this Scheduler scheduler, SchedulerEntity entity)
        {
            entity.Id = scheduler.Id;
            entity.Name = scheduler.Name;

            entity.GoodMultiplier = scheduler.GoodMultiplier;
            entity.EasyMultiplier = scheduler.EasyMultiplier;
            entity.HardMultiplier = scheduler.HardMultiplier;
            entity.AgainDayCount = scheduler.AgainDayCount;
            entity.AgainStageFallback = scheduler.AgainStageFallback;
            entity.GoodOnNewStage = scheduler.GoodOnNewStage;
            entity.EasyOnNewDayCount = scheduler.EasyOnNewDayCount;
            entity.HardOnNewStage = scheduler.HardOnNewStage;
            entity.LearningStages = [..scheduler.LearningStages.Select(ts => ts.Minutes)];

            scheduler.Owner.MapToEntity(entity.User);
        }
        public static void MapToEntity(this User User, UserEntity entity)
        {
            entity.Id = User.Id;
            entity.Username = User.Username;
            entity.HashedPassword = User.HashedPassword;

            // entity.Decks = [..User.Decks.Select(d => d.MapToEntity())];
            // entity.Tags = [..User.Tags.Select(t => t.MapToEntity())];
            // entity.SchedulerPresets = [..User.SchedulerPresets.Select(s => s.MapToEntity())];
        }
        public static void MapToEntity(this Tag Tag, TagEntity entity)
        {
            entity.Id = Tag.Id;
            entity.Name = Tag.Name;
            entity.Color = Tag.Color;

            Tag.Owner.MapToEntity(entity.User);
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
                owner: de.User.MapToDomain(cache),
                created: de.Created,
                scheduler: de.Scheduler.MapToDomain(cache),
                isTemporary: de.IsTemporary,
                childrenDecks: de.ChildrenDecks.Select(d => d.MapToDomain(cache))
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
                learningStages: se.LearningStages.Select(min => TimeSpan.FromMinutes(min)),
                owner: se.User.MapToDomain(cache)
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
                owner: te.User.MapToDomain(cache)
            );
        }
        #endregion
    }
}