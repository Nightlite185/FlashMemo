namespace FlashMemo.Model
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
    public abstract class Card: IEquatable<Card>
    {
        public Card(string frontContent, string? backContent = null)
        {
            FrontContent = frontContent;
            BackContent = backContent;
            Tags = [];

            Created = DateTime.Now;
            LastModified = DateTime.Now;
            NextReview = DateTime.MinValue;
            LastReviewed = DateTime.MinValue;
            Interval = TimeSpan.Zero;

            State = CardState.New;
            IsBuried = false;
            IsSuspended = false;
        }
        
        #region Properties

        public string FrontContent { get; set; }
        public string? BackContent { get; set; }
        public int Id { get; set; }
        public ICollection<Tag> Tags { get; set; }
        public int DeckId { get; set; }
        public bool IsBuried
        {
            get;
            set
            {
                if (field != value)
                {
                    field = value;

                    NextReview = (NextReview.Date == DateTime.Now.Date)
                        ? DateTime.Today.AddDays(1) 
                        : NextReview;
                }
                
                

                else throw new InvalidOperationException($"You were trying to change buried to the same value. Card id: {Id}, isBuried: {IsBuried}"); 
            }
        }
        public bool IsSuspended
        {
            get;
            set
            {
                if (field != value)
                    field = value;
                
                else throw new InvalidOperationException($"You were trying to change suspension to the same value. Card id: {Id}, isSuspended: {IsSuspended}");
            } 
        }
        public CardState State { get; protected set; }
        public TimeSpan TimeTillNextReview => NextReview - DateTime.Now;
        public TimeSpan Interval { get; protected set; }
        public DateTime Created { get; set; }
        public DateTime LastModified { get; set; }
        public DateTime NextReview { get; set; }
        public DateTime LastReviewed { get; set; }
        public int? LearningStage { get; protected set; }
        #endregion

        #region Public Methods
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