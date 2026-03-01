using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using FlashMemo.Helpers;
using FlashMemo.Model.Persistence;

namespace FlashMemo.ViewModel.Wrappers;

public partial class EditableCardVM: ObservableObject, ICardVM
{
    public EditableCardVM(CardEntity card)
    {
        Tags = [..card.Tags.ToVMs()];
        Note = card.Note.ToVM();
        
        this.card = card;
    }

    [ObservableProperty] 
    public partial NoteVM Note { get; set; }
    public ObservableCollection<TagVM> Tags { get; init; } = [];
    public long Id => card.Id;

    public void RevertChanges()
    {
        Note = card.Note.ToVM();

        Tags.Clear();
        Tags.AddRange(card.Tags.ToVMs());
    }

    public CardEntity ToEntity()
    {
        card.Note = Note.ToDomain();
        card.ReplaceTagsWith(Tags.ToEntities());

        return card;
    }

    private readonly CardEntity card;
}