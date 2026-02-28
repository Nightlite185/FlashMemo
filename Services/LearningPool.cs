using System.Collections.ObjectModel;
using FlashMemo.Model.Domain;
using FlashMemo.Model;

namespace FlashMemo.Services;

public class LearningPool<TCard> where TCard: class, ILearningPoolCard
{
    private readonly PriorityQueue<TCard, DateTime> cardPool = new(initialCapacity: 8);
    public void Add(TCard card)
    {
        if (card.IsDueNow || card.Due is null || card.State != CardState.Learning)
            throw new InvalidOperationException(
            "Cannot add a currently due card, or one with state other than CardState.Learning, to the learning pool.");

        cardPool.Enqueue(card, (DateTime)card.Due);
    }
    public TCard? TryPopEarly()
    {
        if (cardPool.TryDequeue(out var card, out _))
            return card;

        return null;
    }
    public void InjectDueInto(Stack<TCard> allCards)
    {
        var dueCards = GetDue();

        for (int i = dueCards.Count - 1; i >= 0 ; i--)
            allCards.Push(dueCards[i]);
    }
    public int Count => cardPool.Count;

    private ReadOnlyCollection<TCard> GetDue()
    {
        List<TCard> dueLearning = [];

        while (cardPool.TryPeek(out var card, out _) && card.IsDueNow)
        {
            dueLearning.Add(card);
            cardPool.Dequeue();
        }

        return dueLearning.AsReadOnly();
    }
}