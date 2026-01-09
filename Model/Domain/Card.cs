namespace FlashMemo.Model.Domain
{
    public enum CardState
    {
        New,
        Learning,
        Review
    }
    public enum Answers
    {
        Again,
        Hard,
        Good,
        Easy
    }
    public class Card: IEquatable<Card>
    {
        public Card(long id) => this.Id = id; // for mapper use only
        #region Properties
        public long Id { get; private init; }
        public long DeckId { get; set; }
        public bool IsBuried { get; protected set; }
        public bool IsSuspended { get; protected set; }
        public CardState State { get; protected set; }
        public bool IsDue => NextReview <= DateTime.Now;
        public TimeSpan TimeTillNextReview => NextReview - DateTime.Now;
        public TimeSpan Interval { get; protected set; }
        public DateTime Created { get; protected set; }
        public DateTime LastModified { get; protected set; }
        public DateTime NextReview { get; protected set; }
        public DateTime LastReviewed { get; protected set; }
        public int? LearningStage { get; protected set; }
        #endregion

        #region Public Methods
        public Card Rehydrate(DateTime created, DateTime lastModified,
                    DateTime nextReview, DateTime lastReviewed, TimeSpan interval, CardState state,
                    int? learningStage, long parentDeckId, bool isBuried, bool isSuspended) // for mapper use only
        {
            Created = created;
            DeckId = parentDeckId;
            LastModified = lastModified;
            NextReview = nextReview;
            LastReviewed = lastReviewed;
            Interval = interval;
            State = state;
            LearningStage = learningStage;
            IsBuried = isBuried;
            IsSuspended = isSuspended;

            return this;
        }
        public void Review(ScheduleInfo s)
        {
            if (!IsDue) throw new InvalidOperationException("Cannot review a card that is not due atm.");

            LastReviewed = DateTime.Now;
            NextReview = LastReviewed.Add(Interval);

            Interval = s.Interval;
            State = s.State;
            LearningStage = s.LearningStage;
        }
        #endregion

        #region Hashcode and Equals

        public override bool Equals(object? obj)
            => obj is Card c && this.Id == c.Id;

        public bool Equals(Card? other) 
            => other is not null && this.Id == other.Id;

        public override int GetHashCode()
            => HashCode.Combine(Id);
        
        #endregion
    }
}