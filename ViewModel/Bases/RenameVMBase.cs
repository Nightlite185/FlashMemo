using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace FlashMemo.ViewModel.Bases;

public abstract partial class RenameVMBase: ObservableObject, IViewModel
{
    private bool isSavingRename;

    [ObservableProperty] public partial string? TempName { get; set; }
    [ObservableProperty] public partial string Name { get; protected set; }
    
    [ObservableProperty] [NotifyCanExecuteChangedFor(nameof(BeginRenameCommand))]
    public partial bool IsRenaming { get; set; }
    protected virtual bool CanRename => !IsRenaming;

    [RelayCommand(CanExecute = nameof(CanRename))]
    private void BeginRename()
    {
        if (!CanRename)
            return;

        IsRenaming = true;
        TempName = Name;
    }

    [RelayCommand]
    private void CancelRename()
    {
        // A submitted save can still succeed after the editor loses focus.
        if (isSavingRename)
            return;

        TempName = null;
        IsRenaming = false;
    }

    public async Task<bool> RenameAsync(Func<string, Task<bool>> save)
    {
        if (!IsRenaming || isSavingRename)
            return false;

        string? newName = TempName;

        if (string.IsNullOrWhiteSpace(newName) || 
            string.Equals(newName, Name, StringComparison.Ordinal))
        {
            CancelRename();
            return false;
        }

        isSavingRename = true;

        try
        {
            if (!await save(newName))
                return false;

            OnRenameCommitted(newName);
            Name = newName;
            TempName = null;
            IsRenaming = false;
            return true;
        }

        finally { isSavingRename = false; }
    }

    protected virtual void OnRenameCommitted(string name) { }

    // User and preset renaming still use the synchronous workflow for now.
    public string CommitRename()
    {
        if (string.IsNullOrEmpty(TempName))
            throw new InvalidOperationException(); //TODO: replace with silent cancellation.

        // TODO: return false or sth when temp is same as the old name

        Name = TempName;
        IsRenaming = false;

        return Name;
    }
}