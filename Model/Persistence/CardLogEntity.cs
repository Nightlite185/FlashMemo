using FlashMemo.Model.Domain;

namespace FlashMemo.Model.Persistence
{
    public enum CardAction { Review, Modify, Delete, Create, Reschedule }
    public class CardLogEntity(): IEntity
    {
        public long Id { get; set; }
        public long? CardId { get; set; }
        public long? UserId { get; set; }
        public UserEntity User { get; set; } = null!;
        public CardEntity Card { get; set; } = null!;
        public CardAction Action { get; set; }
        public Answers? Answer { get; set; }
        public TimeSpan? AnswerTime { get; set; }
        public CardState NewCardState { get; set; }
        public DateTime TimeStamp { get; set; }
    }
}