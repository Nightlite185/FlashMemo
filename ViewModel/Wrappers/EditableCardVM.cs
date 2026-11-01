using AutoMapper;
using CommunityToolkit.Mvvm.ComponentModel;
using FlashMemo.Helpers;
using FlashMemo.Model.Persistence;
using FlashMemo.Services;

namespace FlashMemo.ViewModel.Wrappers;

public partial class EditableCardVM: ObservableObject, ICardVM
{
    /// <summary>Deck needs to be included with card, otherwise it will throw on init.</summary>
    /// <exception cref="NullReferenceException"></exception>
    public EditableCardVM(CardEntity card, INoteComparer noteComparer, IMapper mapper)
    {
        this.noteComparer = noteComparer;
        this.mapper = mapper;
        this.lastSaved = card;

        Note = mapper.Map<Note>(card.Note).ToVM();

        TryRefreshTags(card);
        TryRefreshDeck(card);
    }

    #region properties
    [ObservableProperty] public partial NoteVM Note { get; set; }
    [ObservableProperty] public partial IDeckMeta Deck { get; private set; } = null!;
    public List<TagVM> Tags { get; init; } = [];
    public long Id => lastSaved.Id;
    public long DeckId => lastSaved.DeckId;
    #endregion
    public void RevertChanges()
    {
        Note = mapper.Map<Note>(lastSaved.Note).ToVM();
        TryRefreshTags(lastSaved);
        TryRefreshDeck(lastSaved);
    }

    public void ChangeDeck(IDeckMeta newDeck) => Deck = newDeck;

    public bool IsSameAsSavedNote(string frontText, string backText)
    {
        var saved = noteComparer.FromModel(lastSaved.Note);
        var edited = noteComparer.FromEditor(Note.Type, frontText, backText);

        return noteComparer.AreEqual(saved, edited);
    }

    public CardEntity ToEntity()
    {
        var edited = mapper.Map<CardEntity>(lastSaved);
        edited.Note = mapper.Map<Note>(Note.ToEntity());
        edited.DeckId = Deck.Id;
        edited.Deck = Deck.ToEntity();
        edited.Tags = mapper.Map<List<Tag>>(Tags.ToEntities());
        return edited;
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
        Tags.AddRange(mapper.Map<List<Tag>>(updated.Tags).ToVMs());
    }
    public void UpdateLastSaved(CardEntity card)
    {
        if (card.Id != lastSaved.Id) throw new InvalidOperationException(
            "Cannot update cached card with a card of different id.");

        lastSaved = card;
    }

    private CardEntity lastSaved;
    private readonly INoteComparer noteComparer;
    private readonly IMapper mapper;
}