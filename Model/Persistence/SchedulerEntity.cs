namespace FlashMemo.Model.Persistence
{
    public class SchedulerEntity: IEntity
    {
        public long Id { get; set; }
        public string Name { get; set; } = null!;
        public long UserId { get; set; }
        public long DeckId { get; set; }
        public float GoodMultiplier { get; set; }
        public float EasyMultiplier { get; set; }
        public float HardMultiplier { get; set; }
        public int AgainDayCount { get; set; }
        public ICollection<int> LearningStages { get; set; } = null!; // TO DO: this gotta be mapped to TimeSpan in minutes
        public int AgainStageFallback { get; set; }
        public int GoodOnNewStage { get; set; }
        public int EasyOnNewDayCount { get; set; }
        public int HardOnNewStage { get; set; }
    }
}