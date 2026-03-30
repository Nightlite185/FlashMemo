using CommunityToolkit.Mvvm.ComponentModel;
using FlashMemo.Helpers;
using FlashMemo.Model.Persistence;
using FlashMemo.Services;

namespace FlashMemo.ViewModel.Wrappers;

public partial class EditableCardVM: ObservableObject, ICardVM
{
    /// <summary>Deck needs to be included with card, otherwise it will throw on init.</summary>
    /// <exception cref="NullReferenceException"></exception>
    public EditableCardVM(CardEntity card, INoteComparer noteComparer)
    {
        this.noteComparer = noteComparer;
        this.card = card;

        Note = card.Note.ToVM();

        TryRefreshTags(card);
        TryRefreshDeck(card);
    }

    public void Refresh(CardEntity updated)
    {
        card = updated;
        Note.Refresh(updated.Note);

        TryRefreshDeck(updated);
        TryRefreshTags(updated);
    }
    [ObservableProperty] public partial NoteVM Note { get; set; }
    [ObservableProperty] public partial IDeckMeta Deck { get; private set; } = null!;
    public List<TagVM> Tags { get; init; } = [];
    public long Id => card.Id;
    public long DeckId => card.DeckId;

    public void RevertChanges()
    {
        Note = card.Note.ToVM();

        Tags.Clear();
        Tags.AddRange(card.Tags.ToVMs());
    }

    public void ChangeDeck(IDeckMeta newDeck) => Deck = newDeck;

    public bool IsSameAsSavedNote(string frontText, string backText)
    {
        var saved = noteComparer.FromModel(card.Note);
        var edited = noteComparer.FromEditor(Note.Type, frontText, backText);

        return noteComparer.AreEqual(saved, edited);
    }

    public CardEntity ToEntity()
    {
        card.Note = Note.ToEntity();
        card.DeckId = Deck.Id;

        card.ReplaceTagsWith(Tags.ToEntities());

        return card;
    }

    private void TryRefreshDeck(CardEntity updated)
    {
        if (updated.Deck is null) throw new NullReferenceException(
            "Deck needs to be included with card for this to work.");

        Deck = updated.Deck;
    }
    private void TryRefreshTags(CardEntity updated)
    {
        if (updated.Tags is null) throw new NullReferenceException(
            "Tags need to be included with card to refresh them");

        Tags.Clear();
        Tags.AddRange(updated.Tags.ToVMs());
    }

    private CardEntity card;
    private readonly INoteComparer noteComparer;
}