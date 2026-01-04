using System.Linq.Expressions;
using FlashMemo.Model.Persistence;

namespace FlashMemo.Model
{   
    [Flags]
    public enum CardStateFlags
    {
        New,
        Learning,
        Review
    }
    public class Filters // all those filter props should be in BrowseVM or even FiltersVM (on class-level bc nesting + INPC is hell).
    {
        public Expression<Func<CardEntity, bool>> ToExpression()
        {
            Expression<Func<CardEntity, bool>> query = c => true;

            if (IsBuried is not null)
                query.Combine(c => 
                    c.IsBuried == this.IsBuried);

            if (IsSuspended is not null)
                query.Combine(c => 
                    c.IsSuspended == this.IsSuspended);

            if (IsDue is not null)
                query.Combine(c => 
                    c.NextReview <= DateTime.Now);

            if (TagIds is not null && TagIds.Any())  
                query.Combine(c => 
                    TagIds.Any(t => 
                        c.Tags.Select(t => t.Id)
                        .Contains(t)));

            if (DeckId is not null)
                query.Combine(c => 
                    c.DeckId == this.DeckId);

            return query;
        }
        public bool? IsBuried { get; init; }
        public bool? IsSuspended { get; init; }
        public bool? IsDue { get; init; }
        public IEnumerable<long>? TagIds { get; init; } = [];
        public long? DeckId { get; init; }
        public TimeSpan OverdueBy { get; init; }
        public CardStateFlags? State { get; init; }
        public DateTime? Created { get; init; } // everywhere with datetime I can do like int input box with 0 meaning today, -1 yesterday, and 1 meaning tmrw, Instead of some fancy-ass datetime picker.
        public TimeSpan? Interval { get; init; }
        public DateTime? NextReview { get; init; }
        public DateTime? LastReviewed { get; init; }
        public DateTime? LastModified { get; init; }
    }
}