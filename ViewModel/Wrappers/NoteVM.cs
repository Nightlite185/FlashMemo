using CommunityToolkit.Mvvm.ComponentModel;
using FlashMemo.Model.Persistence;

namespace FlashMemo.ViewModel.Wrappers;

public abstract class NoteVM: ObservableObject
{
    public Note ToDomain()
    {
        return this switch
        {
            StandardNoteVM sn => StandardNote.Create(
                sn.FrontContent, sn.BackContent),

            _ => throw new NotSupportedException()
        };
    }
}

public partial class StandardNoteVM: NoteVM
{
    public StandardNoteVM(StandardNote sn)
    {
        FrontContent = sn.FrontContent;
        BackContent = sn.BackContent;
    }

    public StandardNoteVM()
    {
        FrontContent = "";
        BackContent = "";
    }

    [ObservableProperty] 
    public partial string FrontContent { get; set; }
    
    [ObservableProperty]
    public partial string BackContent { get; set; }
}