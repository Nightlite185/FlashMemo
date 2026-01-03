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
        public static CardEntity MapToEntity(this Card card)
        {
            return new CardEntity
            {
                Id = card.Id,
                FrontContent = card.FrontContent,
                BackContent = card.BackContent,
                Created = card.Created,
                LastModified = card.LastModified,
                NextReview = card.NextReview,
                LastReviewed = card.LastReviewed,
                Interval = card.Interval,
                State = card.State,
                LearningStage = card.LearningStage,
                Deck = card.ParentDeck.MapToEntity(),
                IsBuried = card.IsBuried,
                IsSuspended = card.IsSuspended,
                Tags = [..card.Tags.Select(t => t.MapToEntity())]
            };
        }
        public static DeckEntity MapToEntity(this Deck deck)
        {
            return new DeckEntity
            {
                Id = deck.Id,
                Cards = [..deck.Select(c => c.MapToEntity())],
                Name = deck.Name,
                User = deck.Owner.MapToEntity(),
                Created = deck.Created,
                Scheduler = deck.Scheduler.MapToEntity(),
                IsTemporary = deck.IsTemporary
            };
        }
        public static SchedulerEntity MapToEntity(this Scheduler scheduler)
        {
            return new SchedulerEntity
            {
                Id = scheduler.Id,
                Name = scheduler.Name,

                GoodMultiplier = scheduler.GoodMultiplier,
                EasyMultiplier = scheduler.EasyMultiplier,
                HardMultiplier = scheduler.HardMultiplier,
                AgainDayCount = scheduler.AgainDayCount,
                AgainStageFallback = scheduler.AgainStageFallback,
                GoodOnNewStage = scheduler.GoodOnNewStage,
                EasyOnNewDayCount = scheduler.EasyOnNewDayCount,
                HardOnNewStage = scheduler.HardOnNewStage,
                LearningStages = [..scheduler.LearningStages.Select(ts => ts.Minutes)],
                User = scheduler.Owner.MapToEntity()
            };
        }
        public static UserEntity MapToEntity(this User User)
        {
            return new UserEntity
            {
                Id = User.Id,
                Username = User.Username,
                HashedPassword = User.HashedPassword,
                Decks = [..User.Decks.Select(d => d.MapToEntity())],
                Tags = [..User.Tags.Select(t => t.MapToEntity())],
                SchedulerPresets = [..User.SchedulerPresets.Select(s => s.MapToEntity())]
            };
        }
        public static TagEntity MapToEntity(this Tag Tag)
        {
            return new TagEntity
            {
                Id = Tag.Id,
                Name = Tag.Name,
                Color = Tag.Color,
                User = Tag.Owner.MapToEntity()
            };
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