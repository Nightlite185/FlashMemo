using FlashMemo.Model;
using FlashMemo.Model.Domain;

namespace FlashMemo.Tests.Fakes.Model;

public record FakeScheduleInfoCard: IScheduleInfoCard
{
    public TimeSpan Interval { get; init; }
    public CardState State { get; init; }
    public LearningStage? LearningStage { get; init; }
    public int? DaysOverdue { get; internal set; }
}