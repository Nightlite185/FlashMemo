using FlashMemo.Model;
using FlashMemo.Model.Persistence;
using FlashMemo.ViewModel.Bases;
using FlashMemo.ViewModel.Wrappers;

namespace FlashMemo.ViewModel;

public interface IAsyncVMFactory<TVM> where TVM: IViewModel
{
    Task<TVM> CreateAsync();
}

public interface IVMFactory<TVM> where TVM: IViewModel
{
    TVM Create();
}

public interface ICloseRequest
{
    public event Action? OnCloseRequest;
}


public interface INavRequestSender
{
    public event Func<NavigationRequest, Task>? NavRequested;
}

public interface IFiltrable
{
    public Task ApplyFiltersAsync(Filters filters);
}

public interface IPopupHost
{
    public PopupVMBase? CurrentPopup { get; set; }
}

public interface IDisplayHost
{
    object CurrentDisplay { get; set; }
    event Func<Task>? NotifyRefresh;
}

public interface IDialogClosedHandler
{
    Task OnDialogClosed();
}

public interface IClosedHandler
{
    void OnClosed();
}

public interface IViewModel;

public interface ICardVM
{
    long Id { get; }
    CardEntity ToEntity();
}

public interface IFocusState
{
    Task OnFocusGained();
    void OnFocusLost();
}

public interface ICtxMenuHost
{
    void OnActionExecuted(CtxMenuAction action);
}

public interface ICardsSource<TCard> where TCard: class
{
    IEnumerable<TCard> Cards { get; }
}