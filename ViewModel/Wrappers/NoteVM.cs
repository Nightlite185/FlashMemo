using CommunityToolkit.Mvvm.ComponentModel;
using FlashMemo.Model.Persistence;

namespace FlashMemo.ViewModel.Wrappers;

public abstract class NoteVM: ObservableObject
{
    public Note ToEntity()
    {
        return this switch
        {
            StandardNoteVM sn => sn.ToEntity(),

            _ => throw new NotSupportedException()
        };
    }
}

public partial class StandardNoteVM: NoteVM
{
    public StandardNoteVM(StandardNote sn)
    {
        entity = sn;

        FrontContent = sn.FrontContent;
        BackContent = sn.BackContent;
    }
    public StandardNoteVM()
    {
        FrontContent = "";
        BackContent = "";

        entity = null;
    }

    [ObservableProperty] 
    public partial string FrontContent { get; set; }
    
    [ObservableProperty]
    public partial string BackContent { get; set; }

    new public StandardNote ToEntity()
    {
        if (entity is null)
        {
            return StandardNote.Create(
                FrontContent, BackContent);
        }

        else
        {
            entity.FrontContent = FrontContent;
            entity.BackContent = BackContent;

            return entity;
        }
    }

    private StandardNote? entity;
}