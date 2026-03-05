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
        Deck = card.Deck ?? throw new NullReferenceException(
            "Deck needs to be included with card for this to work.");
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

    public CardEntity ToEntity()
    {
        card.Note = Note.ToEntity();
        card.DeckId = Deck.Id;

        card.ReplaceTagsWith(Tags.ToEntities());

        return card;
    }

    private readonly CardEntity card;
}