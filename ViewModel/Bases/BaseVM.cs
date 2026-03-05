using CommunityToolkit.Mvvm.ComponentModel;
using FlashMemo.Model.Persistence;
using FlashMemo.Services;

namespace FlashMemo.ViewModel.Bases;

public record NavigationRequest; // base request type
public record UserSelectNavRequest(long CurrentUserId): NavigationRequest;
public record BrowseNavRequest(long UserId): NavigationRequest;
public record UserOptionsNavRequest(long UserId): NavigationRequest;
public record EditCardNavRequest(long CardId, long UserId, BaseVM? Sender = null): NavigationRequest;
public record CreateCardNavRequest(IDeckMeta TargetDeck, BaseVM? Sender = null): NavigationRequest;
public record DeckOptionsNavRequest(long DeckId): NavigationRequest;

public abstract class BaseVM(IDomainEventBus bus): ObservableObject, IViewModel, INavRequestSender, IFocusState, IClosedHandler
{
    public event Func<NavigationRequest, Task>? NavRequested;
    public void RegisterNavBubbling(BaseVM vm)
        => vm.NavRequested += NavigateTo;

    protected async Task NavigateTo(NavigationRequest where)
    {
        if (NavRequested is not null)
            await NavRequested(where);
    }

    public async Task OnFocusGained()
    {
        isFocused = true;
        
        if (isDirty)
        {
            await ReloadAsync();
            isDirty = false;
        }
    }
    public void OnFocusLost() => isFocused = false;

    protected virtual async Task OnDomainChanged()
    {
        if (!isFocused)
            isDirty = true;
    }

    public virtual void OnClosed()
    {
        eventBus.DomainChanged -= OnDomainChanged;
    }

    /// <summary>Optional method, must override base to be useful. Do not call base's implementation when overriding.</summary>
    protected virtual async Task ReloadAsync() => await Task.CompletedTask;

    protected bool isFocused = true;
    protected bool isDirty;

    protected readonly IDomainEventBus eventBus = bus;
}