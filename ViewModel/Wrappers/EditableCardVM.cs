using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using FlashMemo.Helpers;
using FlashMemo.Model.Persistence;

namespace FlashMemo.ViewModel.Wrappers;

public partial class EditableCardVM: ObservableObject, ICardVM
{
    /// <summary>Deck needs to be included with card, otherwise it will throw on init.</summary>
    /// <exception cref="NullReferenceException"></exception>
    public EditableCardVM(CardEntity card)
    {
        Tags = [..card.Tags.ToVMs()];
        Note = card.Note.ToVM();
        
        this.card = card;
        Deck = TryGetDeck(card);
    }

    public void Refresh(CardEntity updated)
    {
        card = updated;
        Deck = TryGetDeck(updated);
        Note.Refresh(updated.Note);

        Tags.Clear();
        Tags.AddRange(updated.Tags.ToVMs());
    }

    [ObservableProperty] public partial NoteVM Note { get; set; }
    [ObservableProperty] public partial IDeckMeta Deck { get; private set; }
    public ObservableCollection<TagVM> Tags { get; init; } = [];
    public long Id => card.Id;

    public void RevertChanges()
    {
        Note = card.Note.ToVM();

        Tags.Clear();
        Tags.AddRange(card.Tags.ToVMs());
    }

    public void ChangeDeck(IDeckMeta newDeck) => Deck = newDeck;

    public bool IsSameAsSavedNote(string frontContent, string backContent)
    {
        return Note.Type switch
        {
            NoteTypes.Standard => IsSameSavedStandard(frontContent, backContent),

            _ => throw new NotSupportedException(
                $"Note type '{Note.Type}' is not supported yet.")
        };
    }

    public CardEntity ToEntity()
    {
        card.Note = Note.ToEntity();
        card.DeckId = Deck.Id;

        card.ReplaceTagsWith(Tags.ToEntities());

        return card;
    }

    private bool IsSameSavedStandard(string frontContent, string backContent)
    {
        if (card.Note is not StandardNote saved)
            throw new NotSupportedException(
                $"Expected saved note to be {nameof(StandardNote)} for note type '{Note.Type}'.");

        return string.Equals(frontContent, saved.FrontContent, StringComparison.Ordinal)
            && string.Equals(backContent, saved.BackContent, StringComparison.Ordinal);
    }

    private static Deck TryGetDeck(CardEntity card)
    {
        return card.Deck ?? throw new NullReferenceException(
            "Deck needs to be included with card for this to work.");
    }

    private CardEntity card;
}