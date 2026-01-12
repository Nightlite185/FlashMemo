using System.Linq.Expressions;

namespace FlashMemo.Helpers
{
    public static class ExpressionExtensions
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
    }    
}

