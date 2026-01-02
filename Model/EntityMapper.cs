using FlashMemo.Model.Domain;
using FlashMemo.Model.Persistence;

namespace FlashMemo.Model
{
    public static class EntityMapper
    {
        #region Caching
        private readonly static Dictionary<long, Tag> cachedTags = [];
        private readonly static Dictionary<long, Card> cachedCards = [];
        private readonly static Dictionary<long, Deck> cachedDecks = [];
        private readonly static Dictionary<long, Scheduler> cachedSchedulers = [];
        private readonly static Dictionary<long, User> cachedUsers = [];
        #endregion
        #region Domain to Entity
        public static CardEntity MapToEntity(Card card)
        {
            
        }

        public static DeckEntity MapToEntity(Deck deck)
        {
            
        }

        public static SchedulerEntity MapToEntity(Scheduler scheduler)
        {
            
        }

        public static UserEntity MapToEntity(User User)
        {
            
        }

        public static TagEntity MapToEntity(Tag Tag)
        {
            
        }
        #endregion

        #region Entity to Domain
        public static Card MapToDomain(CardEntity ce)
        {
            if (cachedCards.TryGetValue(ce.Id, out var cachedCard))
                return cachedCard;
            
            return new Card(
                frontContent: ce.FrontContent,
                backContent: ce.BackContent,
                created: ce.Created,
                lastModified: ce.LastModified,
                nextReview: ce.NextReview,
                lastReviewed: ce.LastReviewed,
                interval: ce.Interval,
                state: ce.State,
                learningStage: ce.LearningStage,
                parentDeck: cachedDecks.GetValueOrDefault(ce.Id) ?? ce.Deck.MapToDomain(),
                isBuried: ce.IsBuried,
                isSuspended: ce.IsSuspended,
                tags: [..ce.Tags.Select(t => t.MapToDomain())],
                id: ce.Id
            );
        }
        public static Deck MapToDomain(this DeckEntity de)
        {
            if (cachedDecks.TryGetValue(de.Id, out var cachedDeck))
                return cachedDeck;

            // map it here

            // cache it right after mapping
        }

        public static Scheduler MapToDomain(this SchedulerEntity se)
        {
            if (cachedSchedulers.TryGetValue(se.Id, out var cachedScheduler))
                return cachedScheduler;

            // map it here

            // cache it right after mapping
        }

        public static User MapToDomain(this UserEntity ue)
        {
            if (cachedUsers.TryGetValue(ue.Id, out var cachedUser))
                return cachedUser;

            // map it here

            // cache it right after mapping
        }

        public static Tag MapToDomain(this TagEntity te)
        {
            if (cachedTags.TryGetValue(te.Id, out var cachedTag))
                return cachedTag;

            
            // map it to domain Tag here

            // cachedTags.Add(tag.Id, tag);
        }
        #endregion
    }
}