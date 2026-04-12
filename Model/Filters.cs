using System.Linq.Expressions;
using FlashMemo.Model.Domain;
using FlashMemo.Model.Persistence;
using FlashMemo.Helpers;
using System.Collections.Immutable;

namespace FlashMemo.Model
{
    public record Filters
    {
        public Expression<Func<CardEntity, bool>> ToExpression(byte offset)
        {
            offset.ThrowInvalidOffset();

            Expression<Func<CardEntity, bool>> query = c
                => c.UserId == this.UserId;

            #region bool filters
            if (IsBuried is not null)
                query = query.Combine(c =>
                    c.IsBuried == this.IsBuried);

            if (IsSuspended is not null)
                query = query.Combine(c => 
                    c.IsSuspended == this.IsSuspended);

            if (IsDue is not null)
            {
                var adjustedNow = DateTime.Now
                    .AddHours(-offset);

                query = query.Combine(c =>
                    c.Due.HasValue && 
                    c.Due.Value.Date <= adjustedNow);
            }

            if (!DeckIds.IsEmpty)
                query = query.Combine(c => 
                    DeckIds.Contains(c.DeckId));
            #endregion
            
            #region collection filters
            if (!TagIds.IsEmpty)
                query = query.Combine(c => 
                    TagIds.Any(t => 
                        c.Tags.Select(t => t.Id)
                        .Contains(t)));

            if (!States.IsEmpty)
                query = query.Combine(c => States.Any(s => s == c.State));

            #endregion
            
            #region Numeric filters
            if (OverdueByDays is int days and > 0)
            {
                var today = DateTime.Today;
                var overdueDayStart = today.AddDays(-days);
                var overdueDayEnd = overdueDayStart.AddDays(1);
                
                query = query.Combine(c =>
                    c.Due.HasValue &&
                    c.Due >= overdueDayStart &&
                    c.Due < overdueDayEnd);
            }

            #endregion

            #region DateTime filters

            if (Created is DateTime created)
            {
                var dayStart = created.Date;
                var dayEnd = dayStart.AddDays(1);

                query = query.Combine(c =>
                    c.Created >= dayStart &&
                    c.Created < dayEnd);
            }

            if (Due is DateTime due)
            {
                var dayStart = due.Date;
                var dayEnd = dayStart.AddDays(1);

                query = query.Combine(c =>
                    c.Due.HasValue &&
                    c.Due >= dayStart &&
                    c.Due < dayEnd);
            }

            if (LastReviewed is DateTime lastR)
            {
                var dayStart = lastR.Date;
                var dayEnd = dayStart.AddDays(1);

                query = query.Combine(c =>
                    c.LastReviewed.HasValue &&
                    c.LastReviewed >= dayStart &&
                    c.LastReviewed < dayEnd);
            }

            if (LastModified is DateTime lastM)
            {
                var dayStart = lastM.Date;
                var dayEnd = dayStart.AddDays(1);

                query = query.Combine(c =>
                    c.LastModified.HasValue &&
                    c.LastModified >= dayStart &&
                    c.LastModified < dayEnd);
            }

            #endregion
            
            return query;
        }
        
        public required long UserId { get; init; }
        public bool? IsBuried { get; init; }
        public bool? IsSuspended { get; init; }
        public bool? IsDue { get; init; }
        public ImmutableList<long> TagIds { get; init; } = [];
        public ImmutableList<long> DeckIds { get; init; } = [];
        public bool IncludeChildrenDecks { get; init; } = true;
        public int? OverdueByDays { get; init; }
        public ImmutableList<CardState> States { get; init; } = [];
        public int? IntervalDays { get; init; }
        public DateTime? Created { get; init; }
        public DateTime? Due { get; init; }
        public DateTime? LastReviewed { get; init; }
        public DateTime? LastModified { get; init; }

        private static readonly Filters allCards = new() { UserId = -1 }; // only place where userid is not set. its private tho so noone can access this.
        public static Filters GetEmpty(long userId)
            => allCards with { UserId = userId };
    }
}
