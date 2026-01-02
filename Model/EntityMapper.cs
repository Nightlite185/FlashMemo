using FlashMemo.Model.Domain;
using FlashMemo.Model.Persistence;

namespace FlashMemo.Model
{
    public static class EntityMapper
    {
        // caches being static is an issue for multiple users but whatever for now
        #region Caching
        public static void ClearCache()
        {
            cachedSchedulers.Clear();
            cachedCards.Clear();
            cachedDecks.Clear();
            cachedUsers.Clear();
            cachedTags.Clear();
        }
        private readonly static Dictionary<long, Tag> cachedTags = []; 
        private readonly static Dictionary<long, Card> cachedCards = [];
        private readonly static Dictionary<long, Deck> cachedDecks = [];
        private readonly static Dictionary<long, Scheduler> cachedSchedulers = [];
        private readonly static Dictionary<long, User> cachedUsers = [];
        #endregion
        
        #region Domain to Entity
        public static CardEntity MapToEntity(this Card card)
        {
            
        }

        public static DeckEntity MapToEntity(this Deck deck)
        {
            
        }

        public static SchedulerEntity MapToEntity(this Scheduler scheduler)
        {
            
        }

        public static UserEntity MapToEntity(this User User)
        {
            
        }

        public static TagEntity MapToEntity(this Tag Tag)
        {
            
        }
        #endregion

        #region Entity to Domain
        public static Card MapToDomain(this CardEntity ce)
        {
            if (cachedCards.TryGetValue(ce.Id, out var cachedCard))
                return cachedCard;

            Card card = new(ce.Id); // db's primary key is generated in the domain when creating a genuinely new card, then mapped to persistence.
            cachedCards.Add(ce.Id, card); // same with Deck, User, etc.
            
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
                parentDeck: cachedDecks.GetValueOrDefault(ce.DeckId) ?? ce.Deck.MapToDomain(),
                isBuried: ce.IsBuried,
                isSuspended: ce.IsSuspended,
                tags: [..ce.Tags.Select(t => t.MapToDomain())]
            );
        }
        public static Deck MapToDomain(this DeckEntity de)
        {
            if (cachedDecks.TryGetValue(de.Id, out var cachedDeck))
                return cachedDeck;

            Deck deck = new(de.Id);
            cachedDecks.Add(de.Id, deck);
            
            return deck.Rehydrate(
                cards: [..de.Cards.Select(c => c.MapToDomain())],
                name: de.Name,
                owner: de.Owner.MapToDomain(),
                created: de.Created,
                scheduler: de.Scheduler.MapToDomain(),
                isTemporary: de.IsTemporary
            );
        }
        public static Scheduler MapToDomain(this SchedulerEntity se)
        {
            if (cachedSchedulers.TryGetValue(se.Id, out var cachedScheduler))
                return cachedScheduler;

            Scheduler scheduler = new(se.Id);
            cachedSchedulers.Add(se.Id, scheduler);

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
        public static User MapToDomain(this UserEntity ue)
        {
            if (cachedUsers.TryGetValue(ue.Id, out var cachedUser))
                return cachedUser;

            User user = new(ue.Id);
            cachedUsers.Add(ue.Id, user);

            return user.Rehydrate(
                username: ue.Username,
                hashedPassword: ue.HashedPassword,
                //cfg: ue.Cfg.MapToDomain(), // not implemented yet
                decks: [..ue.Decks.Select(d => d.MapToDomain())],
                tags: [..ue.Tags.Select(t => t.MapToDomain())],
                schedulers: [..ue.SchedulerPresets.Select(s => s.MapToDomain())]
            );
        }
        public static Tag MapToDomain(this TagEntity te)
        {
            if (cachedTags.TryGetValue(te.Id, out var cachedTag))
                return cachedTag;

            Tag tag = new(te.Id);

            cachedTags.Add(tag.Id, tag);
 
            return tag.Rehydrate(
                name: te.Name,
                color: te.Color,
                owner: te.Owner.MapToDomain()
            );
        }
        #endregion
    }
}