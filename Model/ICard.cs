using FlashMemo.Model.Domain;

namespace FlashMemo.Model;
public interface ICard
{
    public long Id { get; }
    public long DeckId { get; }
    public bool IsBuried { get; }
    public bool IsSuspended { get; }
    public DateTime Created { get; }
    public DateTime? LastModified { get; }
    public DateTime? Due { get; }
    public bool IsDueToday => Due?.Date == DateTime.Today;
    public bool IsDueNow => Due <= DateTime.Now;
    public DateTime? LastReviewed { get; }
    public TimeSpan Interval { get; }
    public CardState State { get; }
    public int? LearningStage { get; }
}