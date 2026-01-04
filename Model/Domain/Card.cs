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
        public Card(string frontContent, ICollection<Tag> tags, string? backContent = null) // new fresh card ctor
        {
            Id = IdGetter.Next();
            FrontContent = frontContent;
            BackContent = backContent;
            Tags = [..tags];

            Created = DateTime.Now;
            LastModified = DateTime.Now;
            NextReview = DateTime.MinValue;
            LastReviewed = DateTime.MinValue;
            Interval = TimeSpan.Zero;

            State = CardState.New;
            IsBuried = false;
            IsSuspended = false;
        }
        public Card(long id) => this.Id = id; // for mapper use only
        #region Properties
        public virtual string FrontContent { get; protected set; } = null!;
        public virtual string? BackContent { get; protected set; }
        public long Id { get; private init; } // change this to 'long' and get it from miliseconds rn at creation time.
        public List<Tag> Tags { get; protected set; } = null!;
        public Deck ParentDeck { get; set; } = null!;
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
        public Card Rehydrate(string frontContent, string? backContent, DateTime created, DateTime lastModified,
                    DateTime nextReview, DateTime lastReviewed, TimeSpan interval, CardState state,
                    int? learningStage, Deck parentDeck, bool isBuried, bool isSuspended, ICollection<Tag> tags) // for mapper use only
        {
            FrontContent = frontContent;
            BackContent = backContent;
            Created = created;
            ParentDeck = parentDeck;
            LastModified = lastModified;
            NextReview = nextReview;
            LastReviewed = lastReviewed;
            Interval = interval;
            State = state;
            LearningStage = learningStage;
            IsBuried = isBuried;
            IsSuspended = isSuspended;
            Tags = [..tags];

            return this;
        }
        public void FlipBuried() => IsBuried = !IsBuried;
        public void FlipSuspended() => IsSuspended = !IsSuspended;
        public void Edit(string frontContent, string? backContent = null)
        {
            FrontContent = frontContent;
            BackContent = backContent;

            LastModified = DateTime.Now;
        }
        public void Review(ScheduleInfo s)
        {
            LastReviewed = DateTime.Now;
            NextReview = LastReviewed.Add(Interval);

            Interval = s.Interval;
            State = s.State;
            LearningStage = s.LearningStage;
        }
        public void Reschedule(DateTime newReviewDate, bool keepInterval)
        {
            State = CardState.Review; // reschedule forces state to review, weird outcomes otherwise.
            NextReview = newReviewDate;
            LastModified = DateTime.Now;

            if (!keepInterval)
                Interval += newReviewDate - DateTime.Now;
        }
        public void Reschedule(TimeSpan timeFromNow, bool keepInterval)
        {
            var now = DateTime.Now;

            NextReview = now.Add(timeFromNow);
            LastModified = now;
            State = CardState.Review;

            if (!keepInterval) 
                Interval += timeFromNow;
        }
        public void Postpone(TimeSpan putOffBy, bool keepInterval)
        {
            NextReview = NextReview.Add(putOffBy);
            LastModified = DateTime.Now;
            State = CardState.Review;

            if (!keepInterval) 
                Interval += putOffBy;
        }
        public void Forget()
        {
            State = CardState.New;
            NextReview = DateTime.Now;
            LastModified = DateTime.Now;
            Interval = TimeSpan.MinValue;
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