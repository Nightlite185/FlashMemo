using System.Collections.ObjectModel;
using FlashMemo.Model.Persistence;
using FlashMemo.Model.Domain;

namespace FlashMemo.Services;

public class LearningPool()
{
    private readonly PriorityQueue<CardEntity, DateTime> learningPool = new(initialCapacity: 8);
    public void Add(CardEntity card)
    {
        if (card.IsDueNow || card.Due is null || card.State != CardState.Learning)
            throw new InvalidOperationException(
            "Cannot add a currently due card, or one with state other than CardState.Learning, to the learning pool.");

        learningPool.Enqueue(card, (DateTime)card.Due);
    }
    public void InjectDueInto(Stack<CardEntity> allCards)
    {
        var dueCards = GetDue();

        for (int i = dueCards.Count - 1; i >= 0 ; i--)
            allCards.Push(dueCards[i]);
    }
    private ReadOnlyCollection<CardEntity> GetDue()
    {
        List<CardEntity> dueLearning = [];

        while (learningPool.TryPeek(out var card, out _) && card.IsDueNow)
        {
            dueLearning.Add(card);
            learningPool.Dequeue();
        }

        return dueLearning.AsReadOnly();
    }
}