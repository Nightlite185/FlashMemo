using CommunityToolkit.Mvvm.ComponentModel;
using FlashMemo.Model.Domain;

namespace FlashMemo.ViewModel.Wrappers;

public abstract class NoteVM: ObservableObject
{
    public Note ToDomain()
    {
        return this switch
        {
            StandardNoteVM sn => new StandardNote(
                sn.FrontContent, sn.BackContent),

            _ => throw new NotSupportedException()
        };
    }
}

public partial class StandardNoteVM(StandardNote sn): NoteVM
{
    [ObservableProperty] 
    public partial string FrontContent { get; set; } = sn.FrontContent;
    
    [ObservableProperty]
    public partial string BackContent { get; set; } = sn.BackContent;
}