using CommunityToolkit.Mvvm.ComponentModel;
using FlashMemo.Model.Persistence;

namespace FlashMemo.ViewModel.Wrappers;

public partial class UserVM: ObservableObject, IViewModel
{
    public UserVM(UserEntity user, UserVMStats stats)
    {
        this.user = user;
        Name = user.Name;

        ReadyForReview = stats.ReadyForReview;
        DeckCount = stats.DeckCount;
        CardCount = stats.CardCount;
        Created = stats.Created;
    }

    private readonly UserEntity user;

    #region Public properties
    public long Id => user.Id;

    [ObservableProperty]
    public partial string Name { get; set; }
    
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
}

public readonly struct UserVMStats
{
    public readonly int DeckCount { get; init; }
    public readonly int CardCount { get; init; }
    public readonly int ReadyForReview { get; init; }
    public readonly DateTime Created { get; init; }
}