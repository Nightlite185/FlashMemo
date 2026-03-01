using System.Linq.Expressions;
using System.Text.Json;
using System.Windows;
using FlashMemo.Model.Domain;
using FlashMemo.Model.Persistence;
using FlashMemo.ViewModel;
using FlashMemo.ViewModel.Wrappers;
using Expression = System.Linq.Expressions.Expression;

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

    public static bool Any(this WindowCollection windows, Func<object, bool> predicate)
    {
        foreach (var win in windows)
            if (predicate(win)) return true;

        return false;
    }

    public static IEnumerable<CardVM> ToVMs(this IEnumerable<CardEntity> cards)
        => cards.Select(c => new CardVM(c));

    public static IEnumerable<TagVM> ToVMs(this IEnumerable<Tag> tags)
        => tags.Select(t => new TagVM(t));

    public static IEnumerable<Tag> ToEntities(this IEnumerable<TagVM> tags)
        => tags.Select(t => t.ToEntity());

    public static IEnumerable<CardEntity> ToEntities(this IEnumerable<ICardVM> cards)
        => cards.Select(c => c.ToEntity());

    public static NoteVM ToVM(this Note note)
    {
        if (note is StandardNote sn)
            return new StandardNoteVM(sn);

        throw new NotSupportedException("Only standard note supported for now.");
    }
    
    public static Expression<Func<CardEntity, bool>> OnSameDay(this Expression<Func<CardEntity, bool>> expr, Func<CardEntity, DateTime?> selector, DateTime when)
    {
        var dayStart = when.Date;
        var dayEnd = dayStart.AddDays(1);

        return expr.Combine(c =>
            selector(c) >= dayStart &&
            selector(c) < dayEnd);
    }

    public static T CloneWithJson<T>(this T obj)
    {
        var json = JsonSerializer.Serialize(obj);

        return JsonSerializer.Deserialize<T>(json)!;
    }
}