using FlashMemo.Model.Persistence;

namespace FlashMemo.Model.Domain;

public record CsvExportDTO
{
    public CsvExportDTO(CardEntity card, Note note)
    {
        if (note is not StandardNote sn)
            throw new NotSupportedException("Only standard notes supported for now.");

        FrontContent = sn.FrontContent;
        BackContent = sn.BackContent;
        Deck = card.Deck.Name;

        Tags = string.Join(separator: ',', card.Tags);
        
        IntervalDays = card.Interval.Days.ToString();
        State = card.State.ToString();
        IsSuspended = card.IsSuspended.ToString();
        IsBuried = card.IsBuried.ToString();
        LearningStage = card.LearningStage?.ToString() ?? "N/A";
        CreatedDate = $"{card.Created:dd/MM/yyyy}";
        
        DueDate = (card.Due is not null)
            ? $"{card.Due:dd/MM/yyyy}"
            : "N/A";

        LastModifiedDate = (card.LastModified is not null)
            ? $"{card.LastModified:dd/MM/yyyy}"
            : "N/A";

        LastReviewedDate = (card.LastReviewed is not null)
            ? $"{card.LastReviewed:dd/MM/yyyy}"
            : "N/A";
    }

    public string FrontContent { get; }
    public string BackContent { get; }
    public string Deck { get; }
    public string DueDate { get; }
    public string Tags { get; }
    public string IntervalDays { get; }
    public string State { get; }
    public string IsSuspended { get; }
    public string IsBuried { get; }
    public string LearningStage { get; }
    public string CreatedDate { get; }
    public string LastModifiedDate { get; }
    public string LastReviewedDate { get; }
}