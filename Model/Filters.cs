using System.Linq.Expressions;
using FlashMemo.Model.Domain;
using FlashMemo.Model.Persistence;
using FlashMemo.Helpers;

namespace FlashMemo.Model
{
    public class Filters // all those filter props should be in BrowseVM or even FiltersVM (on class-level bc nesting + INPC is hell).
    {
        public Expression<Func<CardEntity, bool>> ToExpression()
        {
            Expression<Func<CardEntity, bool>> query = c => true;

            #region bool filters
            if (IsBuried is not null)
                query = query.Combine(c =>
                    c.IsBuried == this.IsBuried);

            if (IsSuspended is not null)
                query = query.Combine(c => 
                    c.IsSuspended == this.IsSuspended);

            if (IsDue is not null)
                query = query.Combine(c => 
                    c.Due <= DateTime.Now);
            #endregion
            
            #region collection filters
            if (TagIds is not null && TagIds.Any())  
                query = query.Combine(c => 
                    TagIds.Any(t => 
                        c.Tags.Select(t => t.Id)
                        .Contains(t)));

            if (States is not null && States.Count > 0)
                query = query.Combine(c => States.Any(s => s == c.State));

            #endregion
            
            #region Numeric filters
            if (DeckId is not null)
                query = query.Combine(c => 
                    c.DeckId == this.DeckId);

            if (OverdueByDays is not null)
                query = query.Combine(c => 
                    (c.Due - DateTime.Now).Days == OverdueByDays);

            if (Interval is TimeSpan interval)
                query = query.Combine(c => c.Interval.Days == interval.Days);
            #endregion

            #region DateTime filters
            if (Created is DateTime created)
                query = query.Combine(c => c.Created.Date == created.Date);

            if (Due is DateTime nextR)
                query = query.Combine(c => c.Due.Date == nextR.Date);

            if (LastReviewed is DateTime lastR)
                query = query.Combine(c => c.LastReviewed.Date == lastR.Date);

            if (LastModified is DateTime lastM)
                query = query.Combine(c => c.LastModified.Date == lastM.Date);
            #endregion
            
            return query;
        }
        
        public bool? IsBuried { get; init; }
        public bool? IsSuspended { get; init; }
        public bool? IsDue { get; init; }
        public IEnumerable<long>? TagIds { get; init; } = [];
        public long? DeckId { get; init; }
        public int? OverdueByDays { get; init; } // null => not chosen, 0 => due today, 1 => overdue by 1 day.
        public IReadOnlySet<CardState>? States { get; init; }
        public TimeSpan? Interval { get; init; }
        public DateTime? Created { get; init; } // everywhere with datetime I can do like int input box with 0 meaning today, -1 yesterday, and 1 meaning tmrw, Instead of some fancy-ass datetime picker.
        public DateTime? Due { get; init; }
        public DateTime? LastReviewed { get; init; }
        public DateTime? LastModified { get; init; }
    }
}