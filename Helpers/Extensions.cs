using System.Linq.Expressions;
using FlashMemo.Model.Persistence;
using FlashMemo.ViewModel.Wrappers;

namespace FlashMemo.Helpers;

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

    public static void AddRange<T>(this ICollection<T> col, IEnumerable<T>? toAdd)
    {
        if (toAdd is null) return;
        
        foreach (var item in toAdd)
            col.Add(item);
    }

    public static IEnumerable<CardItemVM> ToVMs(this IEnumerable<CardEntity> cards)
        => cards.Select(c => new CardItemVM(c));

    public static IEnumerable<TagVM> ToVMs(this IEnumerable<Tag> tags)
        => tags.Select(t => new TagVM(t));

    public static IEnumerable<Tag> ToEntities(this IEnumerable<TagVM> tags)
        => tags.Select(t => t.ToEntity());

    public static IEnumerable<CardEntity> ToEntities(this IEnumerable<CardItemVM> cards)
        => cards.Select(c => c.ToEntity());
    
    public static Expression<Func<CardEntity, bool>> OnSameDay(this Expression<Func<CardEntity, bool>> expr, Func<CardEntity, DateTime?> selector, DateTime when)
    {
        var dayStart = when.Date;
        var dayEnd = dayStart.AddDays(1);

        return expr.Combine(c =>
            selector(c) >= dayStart &&
            selector(c) < dayEnd);
    }
}