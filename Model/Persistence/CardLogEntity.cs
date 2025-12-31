using FlashMemo.Model.Domain;

namespace FlashMemo.Model.Persistence
{
    public enum CardAction { Review, Modify, Delete, Create, Reschedule }
    public class CardLogEntity: IEntity
    {
        public int Id { get; set; }
        public int CardId { get; set; }
        public int UserId { get; set; }
        public User User { get; set; } = null!;
        public Card Card { get; set; } = null!;
        public CardAction Action { get; set; }
        public Answers? Answer { get; set; }
        public TimeSpan? AnswerTime { get; set; }
        public CardState NewCardState { get; set; }
        public DateTime TimeStamp { get; set; }
    }
}