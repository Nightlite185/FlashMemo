using FlashMemo.Helpers;
using FlashMemo.Model.Domain;

namespace FlashMemo.Model.Persistence
{
    public enum CardAction { Review, Modify, Reschedule, Bury, Suspend, Relocate, Forget }
    public class CardLog(): IEntity
    {
        public long Id { get; set; }
        public long CardId { get; set; }
        public long? UserId { get; set; }
        public UserEntity User { get; set; } = null!;
        public CardEntity Card { get; set; } = null!;
        public CardAction Action { get; set; }
        public Answers? Answer { get; set; }
        public TimeSpan? AnswerTime { get; set; }
        public CardState NewCardState { get; set; }
        public DateTime TimeStamp { get; set; }

        /// <summary>Deck needs to be included with card; otherwise this won't work</summary>
        public static CardLog CreateReviewLog(CardEntity card, Answers ans, TimeSpan ansTime)
        {
            if (card.Deck is null) throw new ArgumentException(
                "Deck wasn't included with the card", nameof(card));

            return new()
            {
                Id = IdGetter.Next(),
                UserId = card.Deck.UserId,
                Card = card,
                Action = CardAction.Review,
                Answer = ans,
                AnswerTime = ansTime,
                NewCardState = card.State,
                TimeStamp = DateTime.Now,
            };
        }
        public static CardLog CreateLog(CardEntity card, CardAction action)
        {
            return new()
            {
                Id = IdGetter.Next(),
                UserId = card.Deck.UserId,
                Card = card,
                Action = action,
                NewCardState = card.State,
                TimeStamp = DateTime.Now,
            };
        }
    }
}