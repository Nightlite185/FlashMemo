using FlashMemo.Model;
using FlashMemo.ViewModel.Bases;

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

public interface IViewModel;