using System.Linq.Expressions;
using FlashMemo.Model.Domain;
using FlashMemo.Model.Persistence;
using FlashMemo.Helpers;
using System.Collections.Immutable;

namespace FlashMemo.Model
{
    public record Filters
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

            if (!IncludeChildrenDecks)
                query = query.Combine(c => 
                    c.DeckId == this.DeckId);
            #endregion
            
            #region collection filters
            if (TagIds.Length > 0)
                query = query.Combine(c => 
                    TagIds.Any(t => 
                        c.Tags.Select(t => t.Id)
                        .Contains(t)));

            if (States.Length > 0)
                query = query.Combine(c => States.Any(s => s == c.State));

            #endregion
            
            #region Numeric filters

            if (OverdueByDays is int days)
            {
                var today = DateTime.Today;
                
                query = query.Combine(c =>
                    c.Due.HasValue &&
                    (c.Due.Value.Date - today).TotalDays == days);
            }

            if (Interval is TimeSpan interval)
                query = query.Combine(c => c.Interval.Days == interval.Days);
            #endregion

            #region DateTime filters

            if (Created is DateTime created)
                query = query.OnSameDay(c => c.Created, created);

            if (Due is DateTime due)
                query = query.OnSameDay(c => c.Due, due);

            if (LastReviewed is DateTime lastR)
                query = query.OnSameDay(c => c.LastReviewed, lastR);

            if (LastModified is DateTime lastM)
                query = query.OnSameDay(c => c.LastModified, lastM);

            #endregion
            
            return query;
        }
        
        public bool? IsBuried { get; init; }
        public bool? IsSuspended { get; init; }
        public bool? IsDue { get; init; }
        public ImmutableArray<long> TagIds { get; init; } = [];
        public long? DeckId { get; init; }
        public bool IncludeChildrenDecks { get; init; }
        public int? OverdueByDays { get; init; }
        public ImmutableArray<CardState> States { get; init; } = [];
        public TimeSpan? Interval { get; init; }
        public DateTime? Created { get; init; }
        public DateTime? Due { get; init; }
        public DateTime? LastReviewed { get; init; }
        public DateTime? LastModified { get; init; }
    }
}