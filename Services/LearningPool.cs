using FlashMemo.Model.Persistence;

namespace FlashMemo.Services
{
    public class LearningPool()
    {
        private readonly PriorityQueue<CardEntity, DateTime> learningPool = new(initialCapacity: 8);
        public void Add(CardEntity card)
        {
            if (card.IsDueNow) throw new InvalidOperationException(
                "Cannot add a currently due card to the learning pool.");

            learningPool.Enqueue(card, card.Due);
        }
        public void InjectDueInto(Stack<CardEntity> allCards)
        {
            var dueCards = GetDue();

            for (int i = dueCards.Count - 1; i >= 0 ; i--)
                allCards.Push(dueCards[i]);
        }
        private IReadOnlyList<CardEntity> GetDue()
        {
            IList<CardEntity> dueLearning = [];

            while (learningPool.TryPeek(out var card, out _) && card.IsDueNow)
                dueLearning.Add(card);

            return dueLearning.AsReadOnly();
        }
    }
}