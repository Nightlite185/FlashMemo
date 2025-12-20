namespace FlashMemo.Model
{
    public enum CardState
    {
        New,
        Learning,
        Review
    }
    public enum AnswerQuality
    {
        Again,
        Hard,
        Good,
        Easy
    }
    public abstract class Card: IEquatable<Card>
    {
        public Card(float currentMultiplier)
        {
            Created = DateTime.Now;
            LastModified = DateTime.Now;
            Interval = TimeSpan.Zero;
            Multiplier = currentMultiplier;
            State = CardState.New;
            IsBuried = false;
            IsSuspended = false;
            NextReview = DateTime.MinValue;
            LastReviewed = DateTime.MinValue;
        }
        
        #region Properties
        public int Id { get; set; }
        public bool IsBuried { get; set; }
        public bool IsSuspended
        {
            get; 
            protected set
            {
                if (field != value)
                    field = value;
                
                else throw new InvalidOperationException($"You were trying to change suspension to the same value. Card id: {Id}, isSuspended: {IsSuspended}");
            } 
        }
        public CardState State { get; protected set; }
        public float Multiplier { get; set; }
        public TimeSpan Interval { get; set; }
        public DateTime Created { get; set; }
        public DateTime LastModified { get; set; }
        public DateTime NextReview { get; set; }
        public DateTime LastReviewed { get; set; }
        #endregion

        #region Public Methods

        #endregion

        #region Protected Methods
        protected void Ascend()
        {
            if (State == CardState.Review)
                throw new InvalidOperationException($"You were trying to ascend a card that is already in its highest state. Card id: {Id}");

            State++;
        }
        protected void Descend()
        {
            if (State == CardState.New)
                throw new InvalidOperationException($"You were trying to descend a card that is already in its lowest state. Card id: {Id}");

            State--;
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