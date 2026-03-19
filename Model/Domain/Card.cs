namespace FlashMemo.Model.Domain;

public enum CardState { New, Learning, Review }
public enum Answers { Again, Hard, Good, Easy }

public record struct ScheduleInfo(
    TimeSpan Interval, 
    CardState State, 
    LearningStage? LearningStage);

public class Card: ICard
{
    #region Properties
    public long Id { get; private init; }
    public long DeckId { get; set; }
    public bool IsBuried { get; protected set; }
    public bool IsSuspended { get; protected set; }
    private bool IsDueNow => Due <= DateTime.Now;
    private bool IsDueToday => Due.HasValue
        && Due.Value.Date <= DateTime.Today;
    public CardState State { get; protected set; }
    public TimeSpan Interval { get; protected set; }
    public DateTime Created { get; protected set; }
    public DateTime? LastModified { get; protected set; }
    public DateTime? Due { get; protected set; }
    public DateTime? LastReviewed { get; protected set; }
    public LearningStage? LearningStage { get; protected set; }
    #endregion

    #region Methods
    public void Review(ScheduleInfo s)
    {
        Validate();

        LastReviewed = DateTime.Now;
        Due = DateTime.Now.Add(s.Interval);

        Interval = s.Interval;
        State = s.State;
        LearningStage = s.LearningStage;
    }
    private void Validate()
    {
        if (Due is null && State != CardState.New)
            throw new InvalidOperationException(
            $"card's Due property is null, when state wasn't New, but {State}");

        if (State == CardState.Learning && LearningStage is null)
            throw new InvalidOperationException(
            "Card cannot be in learning state when stage is null");

        if (State != CardState.Learning && LearningStage is not null)
            throw new InvalidOperationException(
            $"Card cannot have LearningStage value and be in a state other than Learning. State: {State}, stage: {LearningStage}");

        if (Due is not null && (!IsDueNow && !IsDueToday))
            throw new InvalidOperationException(
            "Cannot review a card that is neither due, nor a new card.");
    }
    #endregion
}