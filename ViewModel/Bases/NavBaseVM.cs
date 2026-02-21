using CommunityToolkit.Mvvm.ComponentModel;
using FlashMemo.Model.Persistence;

namespace FlashMemo.ViewModel.Bases;

public record NavigationRequest; // base request type
public record UserSelectNavRequest(long CurrentUserId): NavigationRequest;
public record BrowseNavRequest(long UserId): NavigationRequest;
public record UserOptionsNavRequest(long UserId): NavigationRequest;
public record EditCardNavRequest(long CardId, long UserId, NavBaseVM? Sender = null): NavigationRequest;
public record CreateCardNavRequest(IDeckMeta TargetDeck, NavBaseVM? Sender = null): NavigationRequest;
public record DeckOptionsNavRequest(long DeckId): NavigationRequest;

public abstract class NavBaseVM: ObservableObject, IViewModel, INavRequestSender
{
    public event Func<NavigationRequest, Task>? NavRequested;
    public void RegisterNavBubbling(NavBaseVM vm)
        => vm.NavRequested += NavigateTo;

    protected async Task NavigateTo(NavigationRequest where)
    {
        if (NavRequested is not null)
            await NavRequested(where);
    }
}