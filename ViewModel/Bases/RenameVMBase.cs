using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace FlashMemo.ViewModel.Bases;

public abstract partial class RenameVMBase: ObservableObject, IViewModel
{
    [ObservableProperty] public partial string? TempName { get; set; }
    [ObservableProperty] public partial string Name { get; protected set; }
    
    [ObservableProperty] [NotifyCanExecuteChangedFor(nameof(BeginRenameCommand))]
    public partial bool IsRenaming { get; set; }
    protected virtual bool CanRename => !IsRenaming;

    [RelayCommand(CanExecute = nameof(CanRename))]
    private void BeginRename()
    {
        IsRenaming = true;
        TempName = Name;
    }

    [RelayCommand]
    private void CancelRename()
    {
        TempName = null;
        IsRenaming = false;
    }

    public string CommitRename()
    {
        if (string.IsNullOrEmpty(TempName))
            throw new InvalidOperationException();

        Name = TempName;
        IsRenaming = false;

        return Name;
    }
}