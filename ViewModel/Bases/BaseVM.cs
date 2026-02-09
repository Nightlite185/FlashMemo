using CommunityToolkit.Mvvm.ComponentModel;
using FlashMemo.Model.Persistence;

namespace FlashMemo.ViewModel.Bases;

public record NavigationRequest; // base request type
public record UserSelectNavRequest: NavigationRequest;
public record BrowseNavRequest(long UserId): NavigationRequest;
public record UserOptionsNavRequest(long UserId): NavigationRequest;
public record EditCardNavRequest(long CardId, long UserId): NavigationRequest;
public record CreateCardNavRequest(IDeckMeta TargetDeck): NavigationRequest;

public abstract class BaseVM: ObservableObject, IViewModel, INavRequestSender
{
    public event Func<NavigationRequest, Task>? NavRequested;

    protected async Task NavigateTo(NavigationRequest where)
    {
        if (NavRequested is not null)
            await NavRequested(where);
    }
}