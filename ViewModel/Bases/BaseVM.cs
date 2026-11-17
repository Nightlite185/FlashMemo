using CommunityToolkit.Mvvm.ComponentModel;
using FlashMemo.Model.Persistence;
using FlashMemo.Services;
using Microsoft.Extensions.Logging;

namespace FlashMemo.ViewModel.Bases;

public abstract record NavigationRequest; // base request type
public record UserSelectNavRequest(long CurrentUserId): NavigationRequest;
public record BrowseNavRequest(long UserId): NavigationRequest;
public record UserOptionsNavRequest(long UserId): NavigationRequest;
public record EditCardNavRequest(long CardId, long UserId): NavigationRequest;
public record CreateCardNavRequest(IDeckMeta TargetDeck): NavigationRequest;
public record DeckOptionsNavRequest(long DeckId): NavigationRequest;

public abstract class BaseVM(IVMEventBus bus, ILogger logger, IDialogService dialogService, SafetyNet net)
    : ObservableObject, IViewModel, INavRequestSender, IFocusState, IClosedHandler
{
    private bool isReloading;

    public event Func<NavigationRequest, Task>? NavRequested;
    public void RegisterNavBubbling(INavRequestSender vm)
        => vm.NavRequested += NavigateTo;

    protected virtual async Task NavigateTo(NavigationRequest where)
    {
        if (NavRequested is not null)
            await NavRequested(where);
    } 

    public virtual async Task OnFocusGained()
    {
        if (isFocused) return;

        isFocused = true;
        
        // Error dialogs can return focus before the original reload has completed.
        if (isReloading) return;
        isReloading = true;

        try
        {
            if (isDomainDirty && await net.TryUnsafe(
                ReloadDomainAsync, "reload the current view's data", retry: true))
                isDomainDirty = false;

            if (isUserOptDirty && await net.TryUnsafe(
                ReloadUserOptAsync, "reload user options", retry: true))
                isUserOptDirty = false;

            if (isDeckOptDirty && await net.TryUnsafe(
                ReloadDeckOptAsync, "reload deck options", retry: true))
                isDeckOptDirty = false;
        }

        finally { isReloading = false; }
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
            isDeckOptDirty = true;
    }

    public virtual void OnClosed()
    {
        eventBus.DomainChanged -= OnDomainChanged;
        eventBus.UserOptionsChanged -= OnUserOptChanged;
        eventBus.DeckOptionsChanged -= OnDeckOptChanged;
    }

    /// <summary>Reloads domain data. Override when this view model needs that refresh.</summary>
    /// <returns>
    /// Normally return true after success (including a handled state that needs no further work),
    /// allowing OnFocusGained to clear isDomainDirty.
    /// Return false only to keep the flag dirty without throwing, for example when another load
    /// is still in progress or a nested SafetyNet already handled a failure.
    /// </returns>
    /// <remarks>
    /// Let failures propagate to OnFocusGained's SafetyNet; they also leave the flag dirty.
    /// Do not catch an exception just to return false.
    /// Returning false does not trigger an error dialog or immediate retry.
    /// The default implementation returns true because this view model has no work for this hook.
    /// </remarks>
    protected virtual Task<bool> ReloadDomainAsync() => Task.FromResult(true);

    /// <summary>Reloads user options. Override when this view model needs that refresh.</summary>
    /// <returns>
    /// Normally return true after success (including a handled state that needs no further work),
    /// allowing OnFocusGained to clear isUserOptDirty.
    /// Return false only to keep the flag dirty without throwing, for example when another load
    /// is still in progress or a nested SafetyNet already handled a failure.
    /// </returns>
    /// <remarks>
    /// Let failures propagate to OnFocusGained's SafetyNet; they also leave the flag dirty.
    /// Do not catch an exception just to return false.
    /// Returning false does not trigger an error dialog or immediate retry.
    /// The default implementation returns true because this view model has no work for this hook.
    /// </remarks>
    protected virtual Task<bool> ReloadUserOptAsync() => Task.FromResult(true);

    /// <summary>Reloads deck options. Override when this view model needs that refresh.</summary>
    /// <returns>
    /// Normally return true after success (including a handled state that needs no further work),
    /// allowing OnFocusGained to clear isDeckOptDirty.
    /// Return false only to keep the flag dirty without throwing, for example when another load
    /// is still in progress or a nested SafetyNet already handled a failure.
    /// </returns>
    /// <remarks>
    /// Let failures propagate to OnFocusGained's SafetyNet; they also leave the flag dirty.
    /// Do not catch an exception just to return false.
    /// Returning false does not trigger an error dialog or immediate retry.
    /// The default implementation returns true because this view model has no work for this hook.
    /// </remarks>
    protected virtual Task<bool> ReloadDeckOptAsync() => Task.FromResult(true);

    protected bool isFocused = true;
    protected bool isDomainDirty;
    protected bool isUserOptDirty;
    protected bool isDeckOptDirty;

    protected readonly IVMEventBus eventBus = bus;
}