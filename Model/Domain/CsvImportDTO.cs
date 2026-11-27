namespace FlashMemo.Model.Domain;

public class CsvImportDTO
{
    public string FrontContent { get; set; } = string.Empty;
    public string BackContent { get; set; } = string.Empty;
    public string? DeckPath { get; set; }
    public string[] Tags { get; set; } = [];

    // Optional metadata stays null when its column is omitted.
    public DateTime? Due { get; set; }
    public TimeSpan? Interval { get; set; }
    public CardState? State { get; set; }
    public bool? IsSuspended { get; set; }
    public bool? IsBuried { get; set; }
    public LearningStage? LearningStage { get; set; }
    public DateTime? Created { get; set; }
    public DateTime? LastModified { get; set; }
    public DateTime? LastReviewed { get; set; }
}