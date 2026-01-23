using System.Linq.Expressions;
using FlashMemo.Model.Persistence;
using FlashMemo.ViewModel;

namespace FlashMemo.Helpers
{
    public static class Extensions
    {
        public static Expression<Func<T, bool>> Combine<T>(
            this Expression<Func<T, bool>> left,
            Expression<Func<T, bool>> right)
        {
            var param = Expression.Parameter(typeof(T));

            var body = Expression.AndAlso(
                Expression.Invoke(left, param),
                Expression.Invoke(right, param)
            );

            return Expression.Lambda<Func<T, bool>>(body, param);
        }

        public static void AddRange<T>(this ICollection<T> col, IEnumerable<T> toAdd)
        {
            foreach (var item in toAdd)
                col.Add(item);
        }

        public static IEnumerable<CardItemVM> TransformToVMs(this IEnumerable<CardEntity> cards)
            => cards.Select(c => new CardItemVM(c));

        public static IEnumerable<CardEntity> TransformToCards(this IEnumerable<CardItemVM> cardsVMs)
            => cardsVMs.Select(vm => vm.Card);
        public static void GenerateNewId (this IEntity entity)
        {
            entity.Id = DateTimeOffset
                .UtcNow
                .ToUnixTimeMilliseconds();
        }
    }    
}