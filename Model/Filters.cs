namespace FlashMemo.Model
{   
    [Flags]
    public enum CardStateFlags
    {
        New,
        Learning,
        Review
    }
    public class Filters
    {
        public bool? Buried { get; set; }
        public bool? Suspended { get; set; }
        public List<Tag>? Tags { get; set; } = [];
        public Deck? Deck { get; set; }
        public CardStateFlags? State { get; set; }
        public DateTime? Created { get; set; } // everywhere with datetime I can do like int input box with 0 meaning today, -1 yesterday, and 1 meaning tmrw, Instead of some fancy-ass datetime picker.
        public TimeSpan? Interval { get; set; }
        public DateTime? NextReview { get; set; }
        public DateTime? LastReviewed { get; set; }
        public DateTime? LastModified { get; set; }
    }
}