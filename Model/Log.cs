namespace FlashMemo.Model
{
    public enum CardAction { Review, Modify, Delete, Create, Reschedule }
    public class CardLog
    {
        public CardLog(CardAction action, Answers? answer, User user, Card card, CardState newState)
        {
            if (action == CardAction.Review && answer == null)
                throw new ArgumentNullException(nameof(answer), "'answer' parameter cannot be null if given CardAction is review.");

            this.Action = action;
            this.Answer = answer;
            this.User = user;
            this.Card = card;
            this.NewCardState = newState;
        }
        public readonly int Id;
        public readonly int CardId;
        public readonly int UserId;
        public readonly User User;
        public readonly Card Card;
        public readonly CardAction Action;
        public readonly Answers? Answer;
        public readonly CardState NewCardState;
        public readonly DateTime TimeStamp = DateTime.Now;
    }
}