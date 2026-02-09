using FlashMemo.Model;
using FlashMemo.ViewModel.Bases;

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

[Obsolete("Dont use since it creates coupling between vm and window.")]
public interface ILoadedHandlerAsync
{
    [Obsolete("Dont use since it creates coupling between vm and window.")]
    public Task LoadEventHandler();
}

public interface INavRequestSender
{
    public event Func<NavigationRequest, Task>? NavRequested;
}

public interface IReloadHandler
{
    public Task ReloadAsync(ReloadTargets rt);
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
    IViewModel CurrentDisplay { get; set; }
    long UserId { get; }
}

public interface IViewModel;