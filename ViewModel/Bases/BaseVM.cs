using System.Printing;
using System.Runtime.CompilerServices;
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

public abstract class BaseVM(IVMEventBus bus, ILogger logger, IDialogService dialogService)
    : ObservableObject, IViewModel, INavRequestSender, IFocusState, IClosedHandler
{
    protected readonly ILogger logger = logger;
    protected readonly IDialogService dialogs = dialogService;

    public event Func<NavigationRequest, Task>? NavRequested;
    public void RegisterNavBubbling(INavRequestSender vm)
        => vm.NavRequested += NavigateTo;

    protected async Task NavigateTo(NavigationRequest where)
    {
        if (NavRequested is not null)
            await NavRequested(where);
    }

    protected async Task<UnsafeActionResult<T>> TryUnsafe<T>(
        Func<Task<T>> action, string actionTried, bool retry,
        [CallerMemberName] string caller = "")
    {
        DialogResult ans = DialogResult.None;
        bool firstPass = true;
        
        while (firstPass || ans is DialogResult.Retry or DialogResult.TryAgain)
        {
            try
            {
                var res = await action();
                return new(true, res);
            }

            catch (Exception e)
            {
                string message = (firstPass ? "Called" : "Retried") + caller;

                logger.LogError(exception: e, message: message);

                ans = dialogs.ShowError(
                    message: "Something went wrong while trying to " + actionTried,
                    buttons: retry ? DialogButtons.RetryCancel : DialogButtons.OK);
            }

            firstPass = false;
        }

        return new(Success: false);
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
            isDeckOptDirty = true;
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

public record struct UnsafeActionResult<T>(
    bool Success, 
    T ResultValue = default!); // mute nullable warning since result prop should be only accessed when success == true.
                               // So its their fault for accessing it with success == false.
                               // nullable T would only cause unnecessary safety checks.