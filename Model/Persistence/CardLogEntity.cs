using FlashMemo.Helpers;
using FlashMemo.Model.Domain;

namespace FlashMemo.Model.Persistence
{
    public enum CardAction { Review, Modify, Reschedule, Bury, Suspend }
    public class CardLogEntity(): IEntity
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
        public static CardLogEntity CreateNew(CardEntity card, CardAction action, Answers? ans, TimeSpan? ansTime)
        {
            return new()
            {
                Id = IdGetter.Next(),
                UserId = card.Deck.UserId,
                Card = card,
                Action = action,
                Answer = ans,
                AnswerTime = ansTime,
                NewCardState = card.State,
                TimeStamp = DateTime.Now,
            };
        }
    }
}