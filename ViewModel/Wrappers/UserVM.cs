using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FlashMemo.Model.Persistence;
using FlashMemo.Repositories;

namespace FlashMemo.ViewModel.Wrappers;

public partial class UserVM: ObservableObject, IViewModel
{
    [Obsolete("TODO: get this repo out of here and replace with event bubbling to parent, requesting commit.")]
    private readonly IUserRepo userRepo;
    public UserVM(UserEntity u, UserVMStats stats, IUserRepo ur)
    {
        userRepo = ur;
        user = u;
        Name = u.Name;

        ReadyForReview = stats.ReadyForReview;
        DeckCount = stats.DeckCount;
        CardCount = stats.CardCount;
        Created = stats.Created;
    }

    private readonly UserEntity user;

    #region Public properties
    public long Id => user.Id;
    
    [ObservableProperty] [NotifyCanExecuteChangedFor(nameof(BeginRenameCommand))]
    public partial bool IsRenaming { get; set; }

    [ObservableProperty]
    public partial string Name { get; set; }

    [ObservableProperty]
    public partial string? TempName { get; set; }
    
    [ObservableProperty]
    public partial int DeckCount { get; private set; }
    
    [ObservableProperty]
    public partial int CardCount { get; private set; }
    
    [ObservableProperty]
    public partial int ReadyForReview { get; private set; }

    [ObservableProperty]
    public partial DateTime Created { get; private set; }

    #endregion

    public UserEntity ToEntity()
    {
        user.Name = Name;
        return user;
    }

    [RelayCommand(CanExecute = nameof(CanRename))]
    private async Task BeginRename()
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

    [RelayCommand]
    private async Task CommitRename()
    {
        if (string.IsNullOrEmpty(TempName))
            throw new InvalidOperationException();

        Name = TempName;
        IsRenaming = false;

        await userRepo.Rename(Id, Name);
    }

    private bool CanRename => !IsRenaming;
}

public readonly struct UserVMStats
{
    public readonly int DeckCount { get; init; }
    public readonly int CardCount { get; init; }
    public readonly int ReadyForReview { get; init; }
    public readonly DateTime Created { get; init; }
}