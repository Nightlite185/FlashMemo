namespace FlashMemo.ViewModel;

public interface ICloseRequest
{
    public event Action? OnCloseRequest;
}
public interface ILoadedHandlerAsync
{
    public Task LoadEventHandler();
}

public interface IViewModel;