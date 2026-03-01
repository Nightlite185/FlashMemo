namespace FlashMemo.Model.Domain;

public abstract record Note;

public record StandardNote(string FrontContent, string BackContent): Note;