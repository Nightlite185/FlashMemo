using FlashMemo.Model.Domain;

namespace FlashMemo.Model;
public interface ICard
{
    long Id { get; }
    long DeckId { get; }
    bool IsBuried { get; }
    bool IsSuspended { get; }
    DateTime Created { get; }
    DateTime? LastModified { get; }
    DateTime? Due { get; }
    bool IsDueToday => Due?.Date == DateTime.Today;
    bool IsDueNow => Due <= DateTime.Now;
    DateTime? LastReviewed { get; }
    TimeSpan Interval { get; }
    CardState State { get; }
    int? LearningStage { get; }
}

public interface IScheduleInfoCard
{
    TimeSpan Interval { get; }
    CardState State { get; }
    int? LearningStage { get; }
}

public interface ILearningPoolCard
{
    DateTime? Due { get; }
    bool IsDueNow => Due <= DateTime.Now;
    CardState State { get; }
}