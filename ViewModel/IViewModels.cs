using FlashMemo.Model;
using FlashMemo.ViewModel.BaseVMs;

namespace FlashMemo.ViewModel;

public interface ICloseRequest
{
    public event Action? OnCloseRequest;
}
public interface ILoadedHandlerAsync
{
    public Task LoadEventHandler();
}

public interface IReloadHandler
{
    public Task ReloadAsync(ReloadTypes rt);
}

public interface IFiltrable
{
    public Task ApplyFiltersAsync(Filters filters);
}

public interface IPopupHost
{
    public PopupVMBase? CurrentPopup { get; set; }
}

public interface IViewModel;