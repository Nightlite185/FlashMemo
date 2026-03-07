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

[Obsolete("Use IFocusState instead")]
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
    Task OnFocusGained(); // TODO: should only change focus state when the focus crosses the window border, NOT JUST UC!!!
    void OnFocusLost(); // example: ReviewVM should not lose focus just because I clicked on the nav bar in MainWindow.
}

public interface ICtxMenuHost
{
    Task OnActionExecuted(CtxMenuAction action);
}

public interface ICardsSource<TCard> where TCard: class
{
    IEnumerable<TCard> Cards { get; }
}

public interface IClosingAware
{
    bool CanClose();
}