namespace FlashMemo.Model.Domain;

public record LearningStages(TimeSpan I, TimeSpan II, TimeSpan III)
{
    public LearningStages(int minutes1, int minutes2, int minutes3): this(
        TimeSpan.FromMinutes(minutes1),
        TimeSpan.FromMinutes(minutes2),
        TimeSpan.FromMinutes(minutes3)
    ){}

    private TimeSpan EnumToTimeSpan(LearningStage? ls)
    {
        return ls switch
        {
            LearningStage.I => I,
            LearningStage.II => II,
            LearningStage.III => III,

            _ => throw new NullReferenceException()
        };
    }

    public TimeSpan this[LearningStage? index]{ 
        get => EnumToTimeSpan(index);}
}

public enum LearningStage { I, II, III }