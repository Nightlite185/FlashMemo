namespace FlashMemo.Model.Domain;

public abstract record Note: INote;

public record StandardNote(string FrontContent, string BackContent): Note;