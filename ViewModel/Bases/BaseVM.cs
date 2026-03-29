using CommunityToolkit.Mvvm.ComponentModel;
using FlashMemo.Model.Persistence;
using FlashMemo.Services;

namespace FlashMemo.ViewModel.Bases;

public abstract record NavigationRequest; // base request type
public record UserSelectNavRequest(long CurrentUserId): NavigationRequest;
public record BrowseNavRequest(long UserId): NavigationRequest;
public record UserOptionsNavRequest(long UserId): NavigationRequest;
public record EditCardNavRequest(long CardId, long UserId): NavigationRequest;
public record CreateCardNavRequest(IDeckMeta TargetDeck): NavigationRequest;
public record DeckOptionsNavRequest(long DeckId): NavigationRequest;

public abstract class BaseVM(IVMEventBus bus): ObservableObject, IViewModel, INavRequestSender, IFocusState, IClosedHandler
{
    public event Func<NavigationRequest, Task>? NavRequested;
    public void RegisterNavBubbling(INavRequestSender vm)
        => vm.NavRequested += NavigateTo;

    protected async Task NavigateTo(NavigationRequest where)
    {
        if (NavRequested is not null)
            await NavRequested(where);
    }

    public virtual async Task OnFocusGained()
    {
        if (isFocused) return;

        isFocused = true;
        
        if (isDomainDirty)
        {
            await ReloadDomainAsync();
            isDomainDirty = false;
        }

        if (isUserOptDirty)
        {
            await ReloadUserOptAsync();
            isUserOptDirty = false;
        }

        if (isDeckOptDirty)
        {
            await ReloadDeckOptAsync();
            isDeckOptDirty = false;
        }
    }
    public virtual void OnFocusLost() => isFocused = false;

    protected virtual void OnDomainChanged()
    {
        if (!isFocused)
            isDomainDirty = true;
    }

    protected virtual void OnUserOptChanged()
    {
        if (!isFocused)
            isUserOptDirty = true;
    }

    protected virtual void OnDeckOptChanged()
    {
        if (!isFocused)
            isUserOptDirty = true;
    }

    public virtual void OnClosed()
    {
        eventBus.DomainChanged -= OnDomainChanged;
        eventBus.UserOptionsChanged -= OnUserOptChanged;
        eventBus.DeckOptionsChanged -= OnDeckOptChanged;
    }

    /// <summary>Optional method, must override base to be useful. Do not call base's implementation when overriding.</summary>
    protected virtual async Task ReloadDomainAsync() => await Task.CompletedTask;

    /// <summary>Optional method, must override base to be useful. Do not call base's implementation when overriding.</summary>
    protected virtual async Task ReloadUserOptAsync() => await Task.CompletedTask;

    /// <summary>Optional method, must override base to be useful. Do not call base's implementation when overriding.</summary>
    protected virtual async Task ReloadDeckOptAsync() => await Task.CompletedTask;

    protected bool isFocused = true;
    protected bool isDomainDirty;
    protected bool isUserOptDirty;
    protected bool isDeckOptDirty;

    protected readonly IVMEventBus eventBus = bus;
}