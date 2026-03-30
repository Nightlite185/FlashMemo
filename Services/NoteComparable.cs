using FlashMemo.Model.Persistence;

namespace FlashMemo.Services;

public abstract record NoteComparable(NoteTypes Type);

public sealed record StandardNoteComparable(string Front, string Back)
    : NoteComparable(NoteTypes.Standard);
